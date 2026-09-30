using CmsApi.Core.Domain.ContentEntities;

namespace CmsApi.Core.Domain.Events.Rules;

public sealed record GroupDecision(
    ContentEntityState? FinalState,
    TombstoneState? FinalTombstone,
    IReadOnlyList<DecidedCmsEvent> DecidedEvents
);

public sealed record DecidedCmsEvent(CmsEvent Event, EventDecision Decision);
