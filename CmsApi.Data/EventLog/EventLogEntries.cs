using CmsApi.Core.Events;
using CmsApi.Core.Events.Rules;
using CmsApi.Core.Events.Validation;

namespace CmsApi.Data.EventLog;

internal static class EventLogEntries
{
    public static EventLogEntry Decided(
        long batchId,
        DecidedCmsEvent decided,
        DateTimeOffset processedAt
    ) =>
        new()
        {
            BatchId = batchId,
            ContentEntityId = decided.Event.Id,
            EventType = decided.Event.Type,
            Version = decided.Event.Version,
            EventTimestamp = decided.Event.Timestamp,
            Outcome = decided.Decision.Outcome,
            Reason = decided.Decision.Reason,
            ProcessedAt = processedAt,
        };

    public static EventLogEntry Failed(
        long batchId,
        FailedCmsEvent failed,
        DateTimeOffset processedAt
    ) =>
        new()
        {
            BatchId = batchId,
            ContentEntityId = failed.Id,
            Outcome = EventOutcome.Failed,
            Reason = failed.Reason,
            RawEvent = failed.RawEvent,
            ProcessedAt = processedAt,
        };
}
