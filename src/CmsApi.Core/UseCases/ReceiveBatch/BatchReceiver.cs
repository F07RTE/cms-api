using CmsApi.Core.Domain.Batches;
using CmsApi.Core.Domain.Inbox;
using CmsApi.Core.Exceptions;

namespace CmsApi.Core.UseCases.ReceiveBatch;

public sealed class BatchReceiver(IInboxRepository inbox, IBatchPublisher publisher)
{
    // A crash after the enqueue leaves a Pending row that was never published. The CMS Client
    // got no 202, so it retries the delivery (ADR 0004).
    public async Task<EnqueuedBatch> ReceiveAsync(
        ReadOnlyMemory<byte> utf8Body,
        CancellationToken cancellationToken
    )
    {
        var body = BatchBodyValidator.Validate(utf8Body);
        var batch = await inbox.EnqueueAsync(body.Text, body.EventCount, cancellationToken);
        await publisher.PublishAsync(batch.BatchId, cancellationToken);
        return batch;
    }
}
