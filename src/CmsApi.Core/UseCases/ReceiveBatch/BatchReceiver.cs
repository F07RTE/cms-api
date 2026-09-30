using CmsApi.Core.Domain.Inbox;
using CmsApi.Core.Exceptions;

namespace CmsApi.Core.UseCases.ReceiveBatch;

/// <summary>
/// Checks a delivered Batch against the whole-body rules and stores it in the Inbox. Each CMS Event
/// is validated later, by the worker.
/// </summary>
public sealed class BatchReceiver(IInboxRepository inbox)
{
    /// <exception cref="InvalidBatchException">The body breaks a whole-body rule.</exception>
    public Task<EnqueuedBatch> ReceiveAsync(
        ReadOnlyMemory<byte> utf8Body,
        CancellationToken cancellationToken
    )
    {
        var body = BatchBodyValidator.Validate(utf8Body);
        return inbox.EnqueueAsync(body.Text, body.EventCount, cancellationToken);
    }
}
