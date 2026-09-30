using CmsApi.Messaging;
using CmsApi.Messaging.Publishing;
using CmsApi.Messaging.Topology;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace CmsApi.IntegrationTests;

public static partial class Orchestrator
{
    public static async Task PurgeQueuesAsync()
    {
        await using var channel = await CreateChannelAsync();
        await channel.QueuePurgeAsync(BatchQueues.Main);
        await channel.QueuePurgeAsync(BatchQueues.Retry);
    }

    // Acks what it reads, so the messages leave the queue.
    public static async Task<List<long>> TakeQueuedBatchIdsAsync()
    {
        await using var channel = await CreateChannelAsync();
        List<long> batchIds = [];
        while (await channel.BasicGetAsync(BatchQueues.Main, autoAck: true) is { } message)
        {
            batchIds.Add(BatchMessage.Deserialize(message.Body).BatchId);
        }

        return batchIds;
    }

    private static async Task<IChannel> CreateChannelAsync()
    {
        var broker = Factory.Services.GetRequiredService<BrokerConnection>();
        var connection = await broker.ConnectAsync(CancellationToken.None);
        return await connection.CreateChannelAsync();
    }
}
