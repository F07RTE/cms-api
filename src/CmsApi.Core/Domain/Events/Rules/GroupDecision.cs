using CmsApi.Core.Domain.ContentEntities;

namespace CmsApi.Core.Domain.Events.Rules;

/// <summary>
/// The decision for each CMS Event of a group, and the state and Tombstone the group leaves behind.
/// </summary>
public sealed record GroupDecision(
    ContentEntityState? FinalState,
    TombstoneState? FinalTombstone,
    IReadOnlyList<DecidedCmsEvent> DecidedEvents
);

public sealed record DecidedCmsEvent(CmsEvent Event, EventDecision Decision);
