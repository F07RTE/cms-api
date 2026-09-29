using CmsApi.Core.Inbox;
using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data.Inbox;

internal sealed class EfInbox(WriteDbContext context, TimeProvider timeProvider) : IInbox
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
        return new ClaimedBatch(batch.Id, batch.Body);
    }

    public Task CompleteAsync(long batchId, CancellationToken cancellationToken)
    {
        var processedAt = timeProvider.GetUtcNow();
        return context
            .InboxBatches.Where(batch => batch.Id == batchId)
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(batch => batch.Status, InboxStatus.Done)
                        .SetProperty(batch => batch.ProcessedAt, processedAt),
                cancellationToken
            );
    }

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
