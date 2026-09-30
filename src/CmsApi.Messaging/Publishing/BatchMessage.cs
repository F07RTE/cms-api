using System.Text.Json;

namespace CmsApi.Messaging.Publishing;

// Only the id travels: the raw Batch stays in the Inbox, so its size never limits the message.
public sealed record BatchMessage(long BatchId)
{
    public const string ContentType = "application/json";

    public byte[] Serialize() =>
        JsonSerializer.SerializeToUtf8Bytes(this, JsonSerializerOptions.Web);

    public static BatchMessage Deserialize(ReadOnlyMemory<byte> body) =>
        JsonSerializer.Deserialize<BatchMessage>(body.Span, JsonSerializerOptions.Web)
        ?? throw new JsonException("The Batch message body is null.");
}
