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
}
