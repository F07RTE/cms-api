using CmsApi.Core.EventLog;
using CmsApi.Core.Events;
using CmsApi.Core.Events.Validation;
using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data.EventLog;

internal sealed class EfEventLog(WriteDbContext context, TimeProvider timeProvider) : IEventLog
{
    public async Task RecordFailedAsync(
        long batchId,
        IReadOnlyList<FailedCmsEvent> failedEvents,
        CancellationToken cancellationToken
    )
    {
        if (failedEvents.Count == 0 || await HasFailedEntriesAsync(batchId, cancellationToken))
        {
            return;
        }

        var processedAt = timeProvider.GetUtcNow();
        context.EventLog.AddRange(
            failedEvents.Select(failed => EventLogEntries.Failed(batchId, failed, processedAt))
        );
        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();
    }

    // The Failed rows of a Batch are saved together, so any one means a replay already has them all.
    private Task<bool> HasFailedEntriesAsync(long batchId, CancellationToken cancellationToken) =>
        context.EventLog.AnyAsync(
            entry => entry.BatchId == batchId && entry.Outcome == EventOutcome.Failed,
            cancellationToken
        );
}
