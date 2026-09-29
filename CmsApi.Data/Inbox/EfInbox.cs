using CmsApi.Core.Inbox;

namespace CmsApi.Data.Inbox;

internal sealed class EfInbox(WriteDbContext context, TimeProvider timeProvider) : IInbox
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
            NextAttemptAt = receivedAt,
        };
        context.InboxBatches.Add(batch);
        await context.SaveChangesAsync(cancellationToken);
        return new EnqueuedBatch(batch.Id, batch.EventCount, batch.ReceivedAt);
    }
}
