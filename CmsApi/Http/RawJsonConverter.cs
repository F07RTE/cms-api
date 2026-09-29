using System.Text.Json;
using System.Text.Json.Serialization;

namespace CmsApi.Http;

/// <summary>Writes a string that already holds JSON as that JSON, with no re-serialisation.</summary>
public sealed class RawJsonConverter : JsonConverter<string>
{
    // Response-only: request bodies are never bound to a raw-JSON string.
    public override string Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) => throw new NotSupportedException();

    // Unvalidated: the text comes from a jsonb column, which only holds valid JSON.
    public override void Write(
        Utf8JsonWriter writer,
        string value,
        JsonSerializerOptions options
    ) => writer.WriteRawValue(value, skipInputValidation: true);
}
