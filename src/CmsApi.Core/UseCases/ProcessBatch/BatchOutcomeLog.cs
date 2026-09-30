using CmsApi.Core.Domain.Events;
using CmsApi.Core.Domain.Events.Rules;
using CmsApi.Core.Domain.Events.Validation;
using Microsoft.Extensions.Logging;

namespace CmsApi.Core.UseCases.ProcessBatch;

/// <summary>Logs what became of each CMS Event of a Batch. Never logs a payload or a raw event.</summary>
public sealed class BatchOutcomeLog(TimeProvider timeProvider, ILogger<BatchOutcomeLog> logger)
{
    private const string ForContentEntityInBatch = "for {ContentEntityId} in Batch {BatchId}";
    private const string IsOutcome = " is {Outcome}: {Reason}";
    private const string FailedTemplate = "CMS Event " + ForContentEntityInBatch + IsOutcome;
    private const string DeleteTemplate =
        "CMS Event {EventType} " + ForContentEntityInBatch + IsOutcome;
    private const string VersionedTemplate =
        "CMS Event {EventType} version {Version} " + ForContentEntityInBatch + IsOutcome;
    private const string FutureTimestampTemplate =
        "CMS Event "
        + ForContentEntityInBatch
        + " has timestamp {Timestamp}, more than {Skew} ahead of the server clock";

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
            LogDecided(batchId, cmsEvent, eventDecision);
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

    // A delete carries no version, so it gets a template without one.
    private void LogDecided(long batchId, CmsEvent cmsEvent, EventDecision decision)
    {
        if (cmsEvent.Version is { } version)
        {
            logger.LogInformation(
                VersionedTemplate,
                cmsEvent.Type,
                version,
                cmsEvent.Id,
                batchId,
                decision.Outcome,
                decision.Reason
            );
            return;
        }

        logger.LogInformation(
            DeleteTemplate,
            cmsEvent.Type,
            cmsEvent.Id,
            batchId,
            decision.Outcome,
            decision.Reason
        );
    }
}
