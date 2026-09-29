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
}
