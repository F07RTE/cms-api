using CmsApi.Core.Domain.Events;
using CmsApi.Core.Domain.Events.Rules;
using CmsApi.Core.Domain.Events.Validation;
using Microsoft.Extensions.Logging;

namespace CmsApi.Core.UseCases.ProcessBatch;

public sealed class BatchOutcomeLog(TimeProvider timeProvider, ILogger<BatchOutcomeLog> logger)
{
    private const string FailedTemplate =
        "CMS Event for {ContentEntityId} in Batch {BatchId} is {Outcome}: {Reason}";

    private const string DecidedEventPart = "CMS Event {EventType}";
    private const string DecidedVersionPart = " version {Version}";
    private const string DecidedOutcomePart =
        " for {ContentEntityId} in Batch {BatchId} is {Outcome}";
    private const string DecidedReasonPart = ": {Reason}";

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
            LogDecided(batchId, cmsEvent, eventDecision);
        }
    }

    private void LogDecided(long batchId, CmsEvent cmsEvent, EventDecision decision)
    {
        var template = DecidedEventPart;
        List<object?> arguments = [cmsEvent.Type];
        if (cmsEvent.Version is { } version)
        {
            template += DecidedVersionPart;
            arguments.Add(version);
        }

        template += DecidedOutcomePart;
        arguments.AddRange([cmsEvent.Id, batchId, decision.Outcome]);
        if (decision.Reason is { } reason)
        {
            template += DecidedReasonPart;
            arguments.Add(reason);
        }

        logger.LogInformation(template, [.. arguments]);
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
