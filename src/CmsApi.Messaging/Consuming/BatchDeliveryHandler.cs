using System.Text.Json;
using CmsApi.Core.UseCases.ConsumeBatch;
using CmsApi.Messaging.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CmsApi.Messaging.Consuming;

public sealed class BatchDeliveryHandler(
    IServiceScopeFactory scopeFactory,
    IOptions<MessagingOptions> options,
    ILogger<BatchDeliveryHandler> logger
)
{
    public async Task HandleAsync(BatchDelivery delivery, CancellationToken cancellationToken)
    {
        // Unacked, it would hold this consumer's only prefetch slot; rejected, it would loop through retry.
        if (TryRead(delivery.Body) is not { } message)
        {
            logger.LogError(
                "Batch message {DeliveryTag} is unreadable; dropping it",
                delivery.DeliveryTag
            );
            await delivery.AckAsync();
            return;
        }

        var attempt = BatchAttempt.AfterFailures(
            message.BatchId,
            delivery.FailedAttempts,
            options.Value.MaxAttempts
        );
        var result = await ConsumeAsync(attempt, cancellationToken);
        await SettleAsync(delivery, result);
    }

    private static BatchMessage? TryRead(ReadOnlyMemory<byte> body)
    {
        try
        {
            return BatchMessage.Deserialize(body);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // A scope per Batch, so each Batch gets a fresh DbContext.
    private async Task<ConsumeResult> ConsumeAsync(
        BatchAttempt attempt,
        CancellationToken cancellationToken
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var consumer = scope.ServiceProvider.GetRequiredService<BatchConsumer>();
        return await consumer.ConsumeAsync(attempt, cancellationToken);
    }

    // Not cancellable: the Batch is already settled in the Inbox, so the broker must hear it.
    private static Task SettleAsync(BatchDelivery delivery, ConsumeResult result) =>
        result == ConsumeResult.RetryLater ? delivery.RejectAsync() : delivery.AckAsync();
}
