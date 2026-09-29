using System.Text.Json;
using CmsApi.Core.ContentEntities;
using CmsApi.Core.EventLog;
using CmsApi.Core.Events;
using CmsApi.Core.Events.Rules;
using CmsApi.Core.Events.Validation;
using CmsApi.Core.Inbox;
using Microsoft.Extensions.Logging;

namespace CmsApi.Core.Batches;

/// <summary>Validates each CMS Event, then applies each Content Entity's group in its own transaction.</summary>
public sealed class BatchProcessor(
    IEventLog eventLog,
    IContentEntityStore contentEntityStore,
    TimeProvider timeProvider,
    ILogger<BatchProcessor> logger
) : IBatchProcessor
{
    public async Task ProcessAsync(ClaimedBatch batch, CancellationToken cancellationToken)
    {
        var validations = Validate(batch.Body);
        await eventLog.RecordFailedAsync(
            batch.BatchId,
            [.. validations.OfType<FailedCmsEvent>()],
            cancellationToken
        );

        var events = validations.OfType<ValidCmsEvent>().Select(valid => valid.Event).ToList();
        WarnAboutFutureTimestamps(batch.BatchId, events);
        foreach (var group in CmsEventOrdering.GroupById(events))
        {
            // Shutdown stops between groups only: a started group always commits.
            cancellationToken.ThrowIfCancellationRequested();
            await contentEntityStore.ApplyGroupAsync(
                batch.BatchId,
                group.ContentEntityId,
                (stored, tombstone) => EventRules.DecideGroup(stored, tombstone, group.Events),
                CancellationToken.None
            );
        }
    }

    private void WarnAboutFutureTimestamps(long batchId, IEnumerable<CmsEvent> events)
    {
        var limit = timeProvider.GetUtcNow() + CmsEventLimits.FutureSkewWarning;
        foreach (var cmsEvent in events.Where(cmsEvent => cmsEvent.Timestamp > limit))
        {
            logger.LogWarning(
                "CMS Event for {ContentEntityId} in Batch {BatchId} has timestamp {Timestamp}, more than {Skew} ahead of the server clock",
                cmsEvent.Id,
                batchId,
                cmsEvent.Timestamp,
                CmsEventLimits.FutureSkewWarning
            );
        }
    }

    // The endpoint already checked the whole-body rules, so the body is a JSON array.
    private static List<CmsEventValidation> Validate(string body)
    {
        using var document = JsonDocument.Parse(body, BatchLimits.ParseOptions);
        return [.. document.RootElement.EnumerateArray().Select(CmsEventValidator.Validate)];
    }
}
