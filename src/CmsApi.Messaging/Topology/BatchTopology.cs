using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CmsApi.Messaging.Topology;

// Declares the queues before the host takes traffic. Declaring is idempotent while the
// arguments match, so every replica does it on start.
public sealed class BatchTopology(BrokerConnection broker, IOptions<MessagingOptions> options)
    : IHostedService
{
    private const string QueueType = "x-queue-type";
    private const string QuorumQueue = "quorum";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var connection = await broker.ConnectAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: cancellationToken
        );
        await DeclareAsync(channel, BatchQueues.Main, MainArguments(), cancellationToken);
        await DeclareAsync(channel, BatchQueues.Retry, RetryArguments(), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static Task DeclareAsync(
        IChannel channel,
        string queue,
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken
    ) =>
        channel.QueueDeclareAsync(
            queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments,
            cancellationToken: cancellationToken
        );

    private static Dictionary<string, object?> MainArguments() =>
        new()
        {
            [QueueType] = QuorumQueue,
            [Headers.XDeadLetterExchange] = BatchQueues.DefaultExchange,
            [Headers.XDeadLetterRoutingKey] = BatchQueues.Retry,
        };

    private Dictionary<string, object?> RetryArguments() =>
        new()
        {
            [Headers.XMessageTTL] = (long)options.Value.RetryDelay.TotalMilliseconds,
            [Headers.XDeadLetterExchange] = BatchQueues.DefaultExchange,
            [Headers.XDeadLetterRoutingKey] = BatchQueues.Main,
        };
}
