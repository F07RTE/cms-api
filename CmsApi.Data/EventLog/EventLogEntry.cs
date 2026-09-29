using CmsApi.Core.Events;

namespace CmsApi.Data.EventLog;

/// <summary>
/// The Event Outcome of one CMS Event. The event columns are null when the element was too
/// broken to read them; <see cref="RawEvent"/> is set only for <see cref="EventOutcome.Failed"/>.
/// </summary>
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
