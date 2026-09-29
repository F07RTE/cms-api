using CmsApi.Core.Inbox;

namespace CmsApi.Worker;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    /// <summary>How long the worker sleeps when the Inbox has nothing due, or another worker leads.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Attempts a Batch gets before it becomes a Dead Batch.</summary>
    public int MaxAttempts { get; set; } = InboxRetryPolicy.DefaultMaxAttempts;
}
