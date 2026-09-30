using CmsApi.Core.Domain.Events;
using CmsApi.Core.Domain.Events.Rules;
using CmsApi.Core.Domain.Events.Validation;
using Microsoft.Extensions.Logging;

namespace CmsApi.Core.UseCases.ProcessBatch;

// Never logs a payload or a raw event.
public sealed class BatchOutcomeLog(TimeProvider timeProvider, ILogger<BatchOutcomeLog> logger)
{
    private const string FailedTemplate =
        "CMS Event for {ContentEntityId} in Batch {BatchId} is {Outcome}: {Reason}";

    // A delete has no version, so {Version} is empty for it.
    private const string DecidedTemplate =
        "CMS Event {EventType} version {Version} for {ContentEntityId} in Batch {BatchId} is {Outcome}: {Reason}";

    private const string FutureTimestampTemplate =
        "CMS Event for {ContentEntityId} in Batch {BatchId} has timestamp {Timestamp}, more than {Skew} ahead of the server clock";

    public void Failed(long batchId, IEnumerable<FailedCmsEvent> failedEvents)
    {
        foreach (var failed in failedEvents)
        {
            logger.LogWarning(
                FailedTemplate,
                failed.Id,
                batchId,
                EventOutcome.Failed,
                failed.Reason
            );
        }
    }

    public void Decided(long batchId, GroupDecision decision)
    {
        foreach (var (cmsEvent, eventDecision) in decision.DecidedEvents)
        {
            logger.LogInformation(
                DecidedTemplate,
                cmsEvent.Type,
                cmsEvent.Version,
                cmsEvent.Id,
                batchId,
                eventDecision.Outcome,
                eventDecision.Reason
            );
        }
    }

    public void FutureTimestamps(long batchId, IEnumerable<CmsEvent> events)
    {
        var limit = timeProvider.GetUtcNow() + CmsEventLimits.FutureSkewWarning;
        foreach (var cmsEvent in events.Where(cmsEvent => cmsEvent.Timestamp > limit))
        {
            logger.LogWarning(
                FutureTimestampTemplate,
                cmsEvent.Id,
                batchId,
                cmsEvent.Timestamp,
                CmsEventLimits.FutureSkewWarning
            );
        }
    }
}
