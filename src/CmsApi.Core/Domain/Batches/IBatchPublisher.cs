namespace CmsApi.Core.Domain.Batches;

public interface IBatchPublisher
{
    // Completes once the broker has taken the Batch, so it's safe to answer 202.
    Task PublishAsync(long batchId, CancellationToken cancellationToken);
}
