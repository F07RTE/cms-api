using CmsApi.Core.Domain.Inbox;

namespace CmsApi.Data.Inbox;

public sealed class InboxBatch
{
    public long Id { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    public required string Body { get; set; }

    public int EventCount { get; set; }

    public InboxStatus Status { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }
}
