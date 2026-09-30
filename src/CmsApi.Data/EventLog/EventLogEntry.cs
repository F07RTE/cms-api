using CmsApi.Core.Domain.Events;

namespace CmsApi.Data.EventLog;

public sealed class EventLogEntry
{
    public long Id { get; set; }

    public long BatchId { get; set; }

    public string? ContentEntityId { get; set; }

    public CmsEventType? EventType { get; set; }

    public long? Version { get; set; }

    public DateTimeOffset? EventTimestamp { get; set; }

    public EventOutcome Outcome { get; set; }

    public string? Reason { get; set; }

    public string? RawEvent { get; set; }

    public DateTimeOffset ProcessedAt { get; set; }
}
