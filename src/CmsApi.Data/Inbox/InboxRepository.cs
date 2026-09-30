using CmsApi.Core.Domain.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace CmsApi.Data.Inbox;

internal sealed class InboxRepository(WriteDbContext context, TimeProvider timeProvider)
    : IInboxRepository
{
    public async Task<EnqueuedBatch> EnqueueAsync(
        string body,
        int eventCount,
        CancellationToken cancellationToken
    )
    {
        var receivedAt = timeProvider.GetUtcNow();
        var batch = new InboxBatch
        {
            ReceivedAt = receivedAt,
            Body = body,
            EventCount = eventCount,
            Status = InboxStatus.Pending,
        };
        context.InboxBatches.Add(batch);
        await context.SaveChangesAsync(cancellationToken);
        return new EnqueuedBatch(batch.Id, batch.EventCount, batch.ReceivedAt);
    }

    public Task<PendingBatch?> FindPendingAsync(
        long batchId,
        CancellationToken cancellationToken
    ) =>
        context
            .InboxBatches.AsNoTracking()
            .Where(batch => batch.Id == batchId && batch.Status == InboxStatus.Pending)
            .Select(batch => new PendingBatch(batch.Id, batch.Body))
            .SingleOrDefaultAsync(cancellationToken);

    public Task CompleteAsync(long batchId, CancellationToken cancellationToken)
    {
        var processedAt = timeProvider.GetUtcNow();
        return UpdateBatchAsync(
            batchId,
            setters =>
                setters
                    .SetProperty(batch => batch.Status, InboxStatus.Done)
                    .SetProperty(batch => batch.ProcessedAt, processedAt),
            cancellationToken
        );
    }

    public Task MarkDeadAsync(
        long batchId,
        string lastError,
        CancellationToken cancellationToken
    ) =>
        UpdateBatchAsync(
            batchId,
            setters =>
                setters
                    .SetProperty(batch => batch.Status, InboxStatus.Dead)
                    .SetProperty(batch => batch.LastError, lastError),
            cancellationToken
        );

    private Task<int> UpdateBatchAsync(
        long batchId,
        Action<UpdateSettersBuilder<InboxBatch>> setters,
        CancellationToken cancellationToken
    ) =>
        context
            .InboxBatches.Where(batch => batch.Id == batchId)
            .ExecuteUpdateAsync(setters, cancellationToken);
}
