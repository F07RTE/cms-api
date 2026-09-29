using CmsApi.Core.Batches;
using Microsoft.Extensions.Logging;

namespace CmsApi.Core.Inbox;

/// <summary>Takes Batches from the Inbox, one at a time. The seam the worker loop and the tests drive.</summary>
public sealed class InboxProcessor(
    IInbox inbox,
    IBatchProcessor batchProcessor,
    TimeProvider timeProvider,
    ILogger<InboxProcessor> logger
)
{
    /// <summary>Attempts a Batch gets before it becomes a Dead Batch.</summary>
    public const int MaxAttempts = 5;

    /// <summary>Claims, processes and completes the next due Batch. False when there was none.</summary>
    public async Task<bool> ProcessNextBatchAsync(CancellationToken cancellationToken)
    {
        var batch = await inbox.ClaimNextAsync(cancellationToken);
        if (batch is null)
        {
            return false;
        }

        try
        {
            await batchProcessor.ProcessAsync(batch, cancellationToken);
            await inbox.CompleteAsync(batch.BatchId, cancellationToken);
        }
        // On shutdown the Batch stays Processing; the next leader recovers it.
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            await FailAsync(batch, exception, cancellationToken);
        }

        return true;
    }

    private Task FailAsync(
        ClaimedBatch batch,
        Exception exception,
        CancellationToken cancellationToken
    ) =>
        batch.Attempts >= MaxAttempts
            ? MarkDeadAsync(batch, exception, cancellationToken)
            : RetryLaterAsync(batch, exception, cancellationToken);

    private Task MarkDeadAsync(
        ClaimedBatch batch,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        logger.LogError(
            exception,
            "Batch {BatchId} is Dead after {Attempts} attempts",
            batch.BatchId,
            batch.Attempts
        );
        return inbox.MarkDeadAsync(batch.BatchId, LastError(exception), cancellationToken);
    }

    private Task RetryLaterAsync(
        ClaimedBatch batch,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var nextAttemptAt = timeProvider.GetUtcNow() + RetryBackoff.After(batch.Attempts);
        logger.LogWarning(
            exception,
            "Batch {BatchId} failed on attempt {Attempts}; retrying at {NextAttemptAt}",
            batch.BatchId,
            batch.Attempts,
            nextAttemptAt
        );
        return inbox.RetryLaterAsync(
            batch.BatchId,
            nextAttemptAt,
            LastError(exception),
            cancellationToken
        );
    }

    private static string LastError(Exception exception) =>
        $"{exception.GetType().Name}: {exception.Message}";
}
