using System.Text;
using RabbitMQ.Client;

namespace CmsApi.Messaging.Consuming;

// The broker's record of every time a message was dead-lettered: one entry per queue and reason.
public static class DeathHeader
{
    public const string Name = "x-death";
    public const string Queue = "queue";
    public const string Count = "count";

    public static long CountFor(IReadOnlyBasicProperties properties, string queue) =>
        properties.Headers is { } headers
        && headers.TryGetValue(Name, out var value)
        && value is IEnumerable<object?> deaths
            ? deaths
                .OfType<IDictionary<string, object?>>()
                .Where(death => IsFor(death, queue))
                .Sum(ReadCount)
            : 0;

    // AMQP carries header strings as bytes.
    private static bool IsFor(IDictionary<string, object?> death, string queue) =>
        death.TryGetValue(Queue, out var value)
        && value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes) == queue,
            string text => text == queue,
            _ => false,
        };

    private static long ReadCount(IDictionary<string, object?> death) =>
        death.TryGetValue(Count, out var value) && value is long count ? count : 0;
}
