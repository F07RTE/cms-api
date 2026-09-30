using CmsApi.Messaging.Topology;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CmsApi.Messaging.Consuming;

// Registered after the topology, so the queue exists before it consumes.
public sealed class BatchQueueConsumer(BrokerConnection broker, BatchDeliveryHandler handler)
    : BackgroundService
{
    // One Batch at a time per replica: scale by adding replicas.
    private const ushort Prefetch = 1;

    private const uint AnyPrefetchSize = 0;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await broker.ConnectAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: stoppingToken
        );
        await channel.BasicQosAsync(AnyPrefetchSize, Prefetch, global: false, stoppingToken);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) =>
            handler.HandleAsync(
                new BatchDelivery(
                    channel,
                    delivery.DeliveryTag,
                    delivery.BasicProperties,
                    delivery.Body
                ),
                stoppingToken
            );
        await channel.BasicConsumeAsync(BatchQueues.Main, autoAck: false, consumer, stoppingToken);
        await WaitForShutdownAsync(stoppingToken);
    }

    // Closing the channel on shutdown returns any unacked message to the queue.
    private static async Task WaitForShutdownAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { }
    }
}
