namespace CmsApi.Core.Events.Rules;

public static class EventRules
{
    private const string UnknownContentEntityReason = "no Content Entity with this id";

    /// <summary>
    /// Applies one CMS Event to the stored state (null when the id is unknown or deleted) and the
    /// Tombstone (null unless deleted).
    /// </summary>
    public static EventDecision Decide(
        ContentEntityState? stored,
        TombstoneState? tombstone,
        CmsEvent cmsEvent
    )
    {
        if (tombstone is not null)
        {
            return new EventDecision(
                EventOutcome.SkippedDeleted,
                $"the Content Entity was deleted at {tombstone.DeletedAt:O}",
                NewState: null,
                tombstone
            );
        }

        return cmsEvent.Type == CmsEventType.Delete
            ? DecideDelete(stored, cmsEvent)
            : DecideVersioned(stored, cmsEvent);
    }

    /// <summary>Decides a group's CMS Events in order, each against the state the previous one left.</summary>
    public static GroupDecision DecideGroup(
        ContentEntityState? stored,
        TombstoneState? tombstone,
        IReadOnlyList<CmsEvent> events
    )
    {
        var state = stored;
        var decided = new List<DecidedCmsEvent>(events.Count);
        foreach (var cmsEvent in events)
        {
            var decision = Decide(state, tombstone, cmsEvent);
            decided.Add(new DecidedCmsEvent(cmsEvent, decision));
            state = decision.NewState;
            tombstone = decision.NewTombstone;
        }

        return new GroupDecision(state, tombstone, decided);
    }

    // A delete applies whatever its timestamp: deletes are CMS truth and ids are never reused.
    private static EventDecision DecideDelete(ContentEntityState? stored, CmsEvent delete) =>
        stored is null
            ? new EventDecision(
                EventOutcome.SkippedUnknown,
                UnknownContentEntityReason,
                NewState: null,
                NewTombstone: null
            )
            : new EventDecision(
                EventOutcome.Applied,
                Reason: null,
                NewState: null,
                new TombstoneState(delete.Timestamp)
            );

    private static EventDecision DecideVersioned(ContentEntityState? stored, CmsEvent cmsEvent)
    {
        var version = RequiredVersion(cmsEvent);
        if (stored is null || IsNewer(stored, version, cmsEvent.Timestamp))
        {
            return Apply(cmsEvent, version);
        }

        return version < stored.Version
            ? new EventDecision(
                EventOutcome.SkippedStale,
                $"version {version} is lower than the stored version {stored.Version}",
                stored,
                NewTombstone: null
            )
            : new EventDecision(
                EventOutcome.SkippedDuplicate,
                $"version {version} is stored, and the timestamp is not newer than {stored.LastEventAt:O}",
                stored,
                NewTombstone: null
            );
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
            ),
            NewTombstone: null
        );

    private static long RequiredVersion(CmsEvent cmsEvent) =>
        cmsEvent.Version ?? throw new ArgumentException("publish and unPublish carry a version.");
}
