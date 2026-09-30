using CmsApi.Core.Domain.Batches;
using CmsApi.Messaging;
using CmsApi.Messaging.Consuming;
using CmsApi.Messaging.Publishing;
using CmsApi.Messaging.Topology;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CmsApi.IntegrationTests;

public static partial class Orchestrator
{
    public static async Task PurgeQueuesAsync()
    {
        await using var channel = await CreateChannelAsync(Factory.Services);
        await channel.QueuePurgeAsync(BatchQueues.Main);
        await channel.QueuePurgeAsync(BatchQueues.Retry);
    }

    public static Task PublishBatchMessageAsync(long batchId) =>
        Factory
            .Services.GetRequiredService<IBatchPublisher>()
            .PublishAsync(batchId, CancellationToken.None);

    public static int MaxAttempts =>
        Factory.Services.GetRequiredService<IOptions<MessagingOptions>>().Value.MaxAttempts;

    public static async Task PublishRetriedBatchMessageAsync(long batchId, long deathCount)
    {
        await using var channel = await CreateChannelAsync(Factory.Services);
        await channel.BasicPublishAsync(
            BatchQueues.DefaultExchange,
            BatchQueues.Main,
            mandatory: false,
            new BasicProperties { Headers = DeathHeaders(deathCount) },
            new BatchMessage(batchId).Serialize()
        );
    }

    public static async Task PublishRawMessageAsync(byte[] body)
    {
        await using var channel = await CreateChannelAsync(Factory.Services);
        await channel.BasicPublishAsync(BatchQueues.DefaultExchange, BatchQueues.Main, body);
    }

    public static async Task<List<long>> TakeQueuedBatchIdsAsync(string queue = BatchQueues.Main)
    {
        await using var channel = await CreateChannelAsync(Factory.Services);
        List<long> batchIds = [];
        while (await channel.BasicGetAsync(queue, autoAck: true) is { } message)
        {
            batchIds.Add(BatchMessage.Deserialize(message.Body).BatchId);
        }

        return batchIds;
    }

    private static async Task<IChannel> CreateChannelAsync(IServiceProvider services)
    {
        var broker = services.GetRequiredService<BrokerConnection>();
        var connection = await broker.ConnectAsync(CancellationToken.None);
        return await connection.CreateChannelAsync();
    }

    private static Dictionary<string, object?> DeathHeaders(long deathCount) =>
        new()
        {
            [BatchDelivery.DeathHeader] = new List<object?>
            {
                new Dictionary<string, object?>
                {
                    [BatchDelivery.DeathQueue] = BatchQueues.Main,
                    [BatchDelivery.DeathCount] = deathCount,
                },
            },
        };
}
