namespace CmsApi.Core.Domain.Inbox;

public interface IInboxRepository
{
    Task<EnqueuedBatch> EnqueueAsync(
        string body,
        int eventCount,
        CancellationToken cancellationToken
    );

    Task<ClaimedBatch?> ClaimNextAsync(CancellationToken cancellationToken);

    Task CompleteAsync(long batchId, CancellationToken cancellationToken);

    Task RetryLaterAsync(
        long batchId,
        DateTimeOffset nextAttemptAt,
        string lastError,
        CancellationToken cancellationToken
    );

    Task MarkDeadAsync(long batchId, string lastError, CancellationToken cancellationToken);

    // Only the leader calls it, so a Processing Batch belongs to a worker that crashed or stopped.
    Task RecoverOrphansAsync(CancellationToken cancellationToken);
}
