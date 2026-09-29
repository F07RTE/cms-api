namespace CmsApi.Core.Events.Rules;

/// <summary>The outcome of one CMS Event and the Content Entity state it leaves behind.</summary>
public sealed record EventDecision(
    EventOutcome Outcome,
    string? Reason,
    ContentEntityState? NewState
);
