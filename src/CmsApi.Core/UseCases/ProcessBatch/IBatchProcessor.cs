using CmsApi.Core.Domain.Inbox;

namespace CmsApi.Core.UseCases.ProcessBatch;

public interface IBatchProcessor
{
    // Invalid CMS Events are recorded as Failed, never thrown: only an infrastructure failure throws.
    Task ProcessAsync(ClaimedBatch batch, CancellationToken cancellationToken);
}
