namespace CmsApi.Core.Inbox;

/// <summary>How many attempts a Batch gets before it becomes a Dead Batch.</summary>
public sealed record InboxRetryPolicy(int MaxAttempts = InboxRetryPolicy.DefaultMaxAttempts)
{
    public const int DefaultMaxAttempts = 5;
}
