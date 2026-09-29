using CmsApi.Core.Inbox;

namespace CmsApi.Data.Inbox;

/// <summary>A Batch in the Inbox. <see cref="Body"/> is the original request text, byte-exact.</summary>
public sealed class InboxBatch
{
    public long Id { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    public required string Body { get; set; }

    public int EventCount { get; set; }

    public InboxStatus Status { get; set; }

    public int Attempts { get; set; }

    public DateTimeOffset NextAttemptAt { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }
}
