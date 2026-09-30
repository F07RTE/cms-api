using CmsApi.Core.Domain.Inbox;
using CmsApi.Core.Exceptions;

namespace CmsApi.Core.UseCases.ReceiveBatch;

public sealed class BatchReceiver(IInboxRepository inbox)
{
    public Task<EnqueuedBatch> ReceiveAsync(
        ReadOnlyMemory<byte> utf8Body,
        CancellationToken cancellationToken
    )
    {
        var body = BatchBodyValidator.Validate(utf8Body);
        return inbox.EnqueueAsync(body.Text, body.EventCount, cancellationToken);
    }
}
