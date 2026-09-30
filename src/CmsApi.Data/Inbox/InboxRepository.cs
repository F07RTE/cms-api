using CmsApi.Core.Domain.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace CmsApi.Data.Inbox;

internal sealed class InboxRepository(WriteDbContext context, TimeProvider timeProvider)
    : IInboxRepository
{
    private const string PendingStatus = nameof(InboxStatus.Pending);

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
            NextAttemptAt = receivedAt,
        };
        context.InboxBatches.Add(batch);
        await context.SaveChangesAsync(cancellationToken);
        return new EnqueuedBatch(batch.Id, batch.EventCount, batch.ReceivedAt);
    }

    public async Task<ClaimedBatch?> ClaimNextAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            cancellationToken
        );
        var batch = await LockNextDueAsync(cancellationToken);
        if (batch is null)
        {
            return null;
        }

        batch.Status = InboxStatus.Processing;
        batch.Attempts++;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        context.ChangeTracker.Clear();
        return new ClaimedBatch(batch.Id, batch.Body, batch.Attempts);
    }

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

    public Task RetryLaterAsync(
        long batchId,
        DateTimeOffset nextAttemptAt,
        string lastError,
        CancellationToken cancellationToken
    ) =>
        UpdateBatchAsync(
            batchId,
            setters =>
                setters
                    .SetProperty(batch => batch.Status, InboxStatus.Pending)
                    .SetProperty(batch => batch.NextAttemptAt, nextAttemptAt)
                    .SetProperty(batch => batch.LastError, lastError),
            cancellationToken
        );

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

    // next_attempt_at is already past, so a recovered Batch is due at once.
    public Task RecoverOrphansAsync(CancellationToken cancellationToken) =>
        context
            .InboxBatches.Where(batch => batch.Status == InboxStatus.Processing)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(batch => batch.Status, InboxStatus.Pending),
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

    private async Task<InboxBatch?> LockNextDueAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var batches = await context
            .InboxBatches.FromSql(
                $"""
                SELECT * FROM inbox
                WHERE status = {PendingStatus}
                  AND next_attempt_at <= {now}
                ORDER BY id
                LIMIT 1
                FOR UPDATE
                """
            )
            .ToListAsync(cancellationToken);
        return batches.SingleOrDefault();
    }
}
