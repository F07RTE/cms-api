namespace CmsApi.Core.Events.Rules;

public static class EventRules
{
    private const string DeleteNotProcessedReason = "delete is not processed yet";

    /// <summary>Applies one CMS Event to the stored state (null when the id is unknown).</summary>
    public static EventDecision Decide(ContentEntityState? stored, CmsEvent cmsEvent)
    {
        if (cmsEvent.Type == CmsEventType.Delete)
        {
            return new EventDecision(EventOutcome.Failed, DeleteNotProcessedReason, stored);
        }

        var version = RequiredVersion(cmsEvent);
        if (stored is null || IsNewer(stored, version, cmsEvent.Timestamp))
        {
            return Apply(cmsEvent, version);
        }

        return version < stored.Version
            ? new EventDecision(
                EventOutcome.SkippedStale,
                $"version {version} is lower than the stored version {stored.Version}",
                stored
            )
            : new EventDecision(
                EventOutcome.SkippedDuplicate,
                $"version {version} is stored, and the timestamp is not newer than {stored.LastEventAt:O}",
                stored
            );
    }

    /// <summary>Decides a group's CMS Events in order, each against the state the previous one left.</summary>
    public static GroupDecision DecideGroup(
        ContentEntityState? stored,
        IReadOnlyList<CmsEvent> events
    )
    {
        var state = stored;
        var decided = new List<DecidedCmsEvent>(events.Count);
        foreach (var cmsEvent in events)
        {
            var decision = Decide(state, cmsEvent);
            decided.Add(new DecidedCmsEvent(cmsEvent, decision));
            state = decision.NewState;
        }

        return new GroupDecision(state, decided);
    }

    // Version alone decides higher or lower; the timestamp only breaks an equal-version tie.
    private static bool IsNewer(
        ContentEntityState stored,
        long version,
        DateTimeOffset timestamp
    ) => version > stored.Version || (version == stored.Version && timestamp > stored.LastEventAt);

    private static EventDecision Apply(CmsEvent cmsEvent, long version) =>
        new(
            EventOutcome.Applied,
            Reason: null,
            new ContentEntityState(
                version,
                cmsEvent.Payload
                    ?? throw new ArgumentException("publish and unPublish carry a payload."),
                IsPublished: cmsEvent.Type == CmsEventType.Publish,
                cmsEvent.Timestamp
            )
        );

    private static long RequiredVersion(CmsEvent cmsEvent) =>
        cmsEvent.Version ?? throw new ArgumentException("publish and unPublish carry a version.");
}
