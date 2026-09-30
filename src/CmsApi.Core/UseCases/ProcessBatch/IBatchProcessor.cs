using CmsApi.Core.Domain.Inbox;

namespace CmsApi.Core.UseCases.ProcessBatch;

public interface IBatchProcessor
{
    /// <summary>Records an Event Outcome for every CMS Event in the Batch. Invalid CMS Events never throw.</summary>
    Task ProcessAsync(ClaimedBatch batch, CancellationToken cancellationToken);
}
