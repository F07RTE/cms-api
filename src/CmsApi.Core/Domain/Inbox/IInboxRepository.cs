namespace CmsApi.Core.Domain.Inbox;

public interface IInboxRepository
{
    Task<EnqueuedBatch> EnqueueAsync(
        string body,
        int eventCount,
        CancellationToken cancellationToken
    );

    Task<PendingBatch?> FindPendingAsync(long batchId, CancellationToken cancellationToken);

    Task CompleteAsync(long batchId, CancellationToken cancellationToken);

    Task MarkDeadAsync(long batchId, string lastError, CancellationToken cancellationToken);
}
