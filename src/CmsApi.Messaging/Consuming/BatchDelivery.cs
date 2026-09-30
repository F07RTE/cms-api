using System.Text;
using CmsApi.Messaging.Topology;
using RabbitMQ.Client;

namespace CmsApi.Messaging.Consuming;

public sealed record BatchDelivery(
    IChannel Channel,
    ulong DeliveryTag,
    IReadOnlyBasicProperties Properties,
    ReadOnlyMemory<byte> Body
)
{
    public const string DeathHeader = "x-death";
    public const string DeathQueue = "queue";
    public const string DeathCount = "count";

    // Each rejection from the main queue is one failed attempt.
    public long FailedAttempts =>
        Properties.Headers is { } headers
        && headers.TryGetValue(DeathHeader, out var value)
        && value is IEnumerable<object?> deaths
            ? deaths.OfType<IDictionary<string, object?>>().Where(IsForMainQueue).Sum(ReadCount)
            : 0;

    public Task AckAsync() => Channel.BasicAckAsync(DeliveryTag, multiple: false).AsTask();

    // Dead-lettered into the retry queue, which routes it back after RetryDelay.
    public Task RejectAsync() => Channel.BasicRejectAsync(DeliveryTag, requeue: false).AsTask();

    // AMQP carries header strings as bytes.
    private static bool IsForMainQueue(IDictionary<string, object?> death) =>
        death.TryGetValue(DeathQueue, out var value)
        && value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes) == BatchQueues.Main,
            string text => text == BatchQueues.Main,
            _ => false,
        };

    private static long ReadCount(IDictionary<string, object?> death) =>
        death.TryGetValue(DeathCount, out var value) && value is long count ? count : 0;
}
