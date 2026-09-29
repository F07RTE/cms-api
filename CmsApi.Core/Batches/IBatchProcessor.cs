using CmsApi.Core.Inbox;

namespace CmsApi.Core.Batches;

public interface IBatchProcessor
{
    /// <summary>Records an Event Outcome for every CMS Event in the Batch. Invalid CMS Events never throw.</summary>
    Task ProcessAsync(ClaimedBatch batch, CancellationToken cancellationToken);
}
