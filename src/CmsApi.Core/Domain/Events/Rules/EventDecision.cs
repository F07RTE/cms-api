using CmsApi.Core.Domain.ContentEntities;

namespace CmsApi.Core.Domain.Events.Rules;

public sealed record EventDecision(
    EventOutcome Outcome,
    string? Reason,
    ContentEntityState? NewState,
    TombstoneState? NewTombstone
);
