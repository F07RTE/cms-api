using System.Text.Json;
using CmsApi.Core.Domain.Batches;
using CmsApi.Core.Domain.ContentEntities;
using CmsApi.Core.Domain.EventLog;
using CmsApi.Core.Domain.Events;
using CmsApi.Core.Domain.Events.Rules;
using CmsApi.Core.Domain.Events.Validation;
using CmsApi.Core.Domain.Inbox;

namespace CmsApi.Core.UseCases.ProcessBatch;

public sealed class BatchProcessor(
    IEventLogRepository eventLog,
    IContentEntityRepository contentEntities,
    BatchOutcomeLog outcomeLog
) : IBatchProcessor
{
    public async Task ProcessAsync(ClaimedBatch batch, CancellationToken cancellationToken)
    {
        var validations = Validate(batch.Body);
        List<FailedCmsEvent> failedEvents = [.. validations.OfType<FailedCmsEvent>()];
        // A replay finds its Failed rows already recorded, and already logged.
        if (await eventLog.RecordFailedAsync(batch.BatchId, failedEvents, cancellationToken))
        {
            outcomeLog.Failed(batch.BatchId, failedEvents);
        }

        var events = validations.OfType<ValidCmsEvent>().Select(valid => valid.Event).ToList();
        outcomeLog.FutureTimestamps(batch.BatchId, events);
        await ApplyGroupsAsync(batch.BatchId, events, cancellationToken);
    }

    private async Task ApplyGroupsAsync(
        long batchId,
        IEnumerable<CmsEvent> events,
        CancellationToken cancellationToken
    )
    {
        foreach (var group in CmsEventOrdering.GroupById(events))
        {
            // Shutdown stops between groups only: a started group always commits.
            cancellationToken.ThrowIfCancellationRequested();
            var decision = await contentEntities.ApplyGroupAsync(
                batchId,
                group.ContentEntityId,
                (stored, tombstone) => EventRules.DecideGroup(stored, tombstone, group.Events),
                CancellationToken.None
            );
            outcomeLog.Decided(batchId, decision);
        }
    }

    // The endpoint already checked the whole-body rules, so the body is a JSON array.
    private static List<CmsEventValidation> Validate(string body)
    {
        using var document = JsonDocument.Parse(body, BatchLimits.ParseOptions);
        return [.. document.RootElement.EnumerateArray().Select(CmsEventValidator.Validate)];
    }
}
