using CmsApi.Core.Batches;

namespace CmsApi.Core.Inbox;

/// <summary>Takes Batches from the Inbox, one at a time. The seam the worker loop and the tests drive.</summary>
public sealed class InboxProcessor(IInbox inbox, IBatchProcessor batchProcessor)
{
    /// <summary>Claims, processes and completes the next due Batch. False when there was none.</summary>
    public async Task<bool> ProcessNextBatchAsync(CancellationToken cancellationToken)
    {
        var batch = await inbox.ClaimNextAsync(cancellationToken);
        if (batch is null)
        {
            return false;
        }

        await batchProcessor.ProcessAsync(batch, cancellationToken);
        await inbox.CompleteAsync(batch.BatchId, cancellationToken);
        return true;
    }
}
