namespace CmsApi.Core.Inbox;

/// <summary>Where a received Batch waits until the worker processes it.</summary>
public interface IInbox
{
    /// <summary>Stores the raw Batch as <see cref="InboxStatus.Pending"/>, ready to process now.</summary>
    Task<EnqueuedBatch> EnqueueAsync(
        string body,
        int eventCount,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Moves the oldest due <see cref="InboxStatus.Pending"/> Batch to
    /// <see cref="InboxStatus.Processing"/> and counts the attempt. Null when none is due.
    /// </summary>
    Task<ClaimedBatch?> ClaimNextAsync(CancellationToken cancellationToken);

    /// <summary>Marks a claimed Batch <see cref="InboxStatus.Done"/>.</summary>
    Task CompleteAsync(long batchId, CancellationToken cancellationToken);

    /// <summary>Puts a failed Batch back to <see cref="InboxStatus.Pending"/>, due at <paramref name="nextAttemptAt"/>.</summary>
    Task RetryLaterAsync(
        long batchId,
        DateTimeOffset nextAttemptAt,
        string lastError,
        CancellationToken cancellationToken
    );

    /// <summary>Marks a Batch <see cref="InboxStatus.Dead"/>. It is never retried automatically.</summary>
    Task MarkDeadAsync(long batchId, string lastError, CancellationToken cancellationToken);

    /// <summary>
    /// Puts every <see cref="InboxStatus.Processing"/> Batch back to <see cref="InboxStatus.Pending"/>.
    /// Only the leader calls it, so those Batches belong to a worker that crashed or stopped.
    /// </summary>
    Task RecoverOrphansAsync(CancellationToken cancellationToken);
}
