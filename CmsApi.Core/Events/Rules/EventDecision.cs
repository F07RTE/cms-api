namespace CmsApi.Core.Events.Rules;

/// <summary>
/// The outcome of one CMS Event and what it leaves behind: the Content Entity state, or its
/// Tombstone once deleted.
/// </summary>
public sealed record EventDecision(
    EventOutcome Outcome,
    string? Reason,
    ContentEntityState? NewState,
    TombstoneState? NewTombstone
);
