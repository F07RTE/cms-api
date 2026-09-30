using CmsApi.Core.Domain.Batches;
using CmsApi.Messaging.Topology;
using RabbitMQ.Client;

namespace CmsApi.Messaging.Publishing;

public sealed class BatchPublisher(BrokerConnection broker) : IBatchPublisher
{
    private static readonly CreateChannelOptions ConfirmedChannel = new(
        publisherConfirmationsEnabled: true,
        publisherConfirmationTrackingEnabled: true
    );

    // A channel per publish: requests publish concurrently, and a channel isn't meant to be shared.
    public async Task PublishAsync(long batchId, CancellationToken cancellationToken)
    {
        var connection = await broker.ConnectAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            ConfirmedChannel,
            cancellationToken
        );
        await channel.BasicPublishAsync(
            BatchQueues.DefaultExchange,
            BatchQueues.Main,
            mandatory: true,
            new BasicProperties { Persistent = true, ContentType = BatchMessage.ContentType },
            new BatchMessage(batchId).Serialize(),
            cancellationToken
        );
    }
}
