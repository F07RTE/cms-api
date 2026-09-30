using CmsApi.Core.Domain.Inbox;
using CmsApi.Core.UseCases.ProcessBatch;
using Microsoft.Extensions.Logging;

namespace CmsApi.Core.UseCases.ConsumeBatch;

public sealed class BatchConsumer(
    IInboxRepository inbox,
    IBatchProcessor batchProcessor,
    ILogger<BatchConsumer> logger
)
{
    public async Task<ConsumeResult> ConsumeAsync(
        BatchAttempt attempt,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await ProcessIfPendingAsync(attempt.BatchId, cancellationToken);
        }
        // On shutdown the message stays unacked, so the broker redelivers it.
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            return attempt.CanRetry
                ? RetryLater(attempt, exception)
                : await MarkDeadAsync(attempt, exception, cancellationToken);
        }
    }

    // A redelivery of a Batch already Done or Dead finds no Pending row, and is skipped.
    private async Task<ConsumeResult> ProcessIfPendingAsync(
        long batchId,
        CancellationToken cancellationToken
    )
    {
        var batch = await inbox.FindPendingAsync(batchId, cancellationToken);
        if (batch is null)
        {
            logger.LogInformation("Batch {BatchId} is not Pending; skipping it", batchId);
            return ConsumeResult.Done;
        }

        await batchProcessor.ProcessAsync(batch, cancellationToken);
        await inbox.CompleteAsync(batchId, cancellationToken);
        return ConsumeResult.Done;
    }

    private ConsumeResult RetryLater(BatchAttempt attempt, Exception exception)
    {
        logger.LogWarning(
            exception,
            "Batch {BatchId} failed on attempt {Attempt}; retrying later",
            attempt.BatchId,
            attempt.Number
        );
        return ConsumeResult.RetryLater;
    }

    private async Task<ConsumeResult> MarkDeadAsync(
        BatchAttempt attempt,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var lastError = $"{exception.GetType().Name}: {exception.Message}";
            await inbox.MarkDeadAsync(attempt.BatchId, lastError, cancellationToken);
        }
        // Left unsettled, the message would hold the consumer's only prefetch slot. Still Pending,
        // the Batch comes back from the retry queue, and that attempt tries to mark it Dead again.
        catch (Exception markDeadFailure) when (!cancellationToken.IsCancellationRequested)
        {
            return RetryLater(attempt, markDeadFailure);
        }

        logger.LogError(
            exception,
            "Batch {BatchId} is Dead after {Attempt} attempts",
            attempt.BatchId,
            attempt.Number
        );
        return ConsumeResult.Dead;
    }
}
