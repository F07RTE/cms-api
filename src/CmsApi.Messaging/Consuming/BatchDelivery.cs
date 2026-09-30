using CmsApi.Messaging.Topology;
using RabbitMQ.Client;

namespace CmsApi.Messaging.Consuming;

// One message taken from cms.batches, pushed to the consumer or pulled by the test drain.
public sealed record BatchDelivery(
    IChannel Channel,
    ulong DeliveryTag,
    IReadOnlyBasicProperties Properties,
    ReadOnlyMemory<byte> Body
)
{
    // Each rejection from the main queue is one failed attempt.
    public long FailedAttempts => DeathHeader.CountFor(Properties, BatchQueues.Main);

    public Task AckAsync() => Channel.BasicAckAsync(DeliveryTag, multiple: false).AsTask();

    // Dead-lettered into the retry queue, which routes it back after RetryDelay.
    public Task RejectAsync() => Channel.BasicRejectAsync(DeliveryTag, requeue: false).AsTask();
}
