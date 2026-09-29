using System.Text;
using System.Text.Json;

namespace CmsApi.Core.Events.Validation;

/// <summary>Turns one raw element of a Batch into a valid CMS Event, or a Failed one with the reason.</summary>
public static class CmsEventValidator
{
    private const string IdProperty = "id";
    private const string TypeProperty = "type";
    private const string VersionProperty = "version";
    private const string TimestampProperty = "timestamp";
    private const string PayloadProperty = "payload";

    private const string NotAnObjectReason = "not a JSON object";
    private static readonly string InvalidIdReason =
        $"id must be a string of 1 to {CmsEventLimits.MaxIdLength} characters, without control characters or surrounding whitespace";
    private const string DuplicateKeyReason = "duplicate key";
    private const string UnknownTypeReason = "unknown type";
    private const string InvalidTimestampReason = "timestamp must be ISO 8601 with an offset";
    private static readonly string InvalidVersionReason =
        $"version must be an integer of at least {CmsEventLimits.MinVersion}";
    private const string PayloadNotObjectReason = "payload must be a JSON object";
    private static readonly string PayloadTooLargeReason =
        $"payload is larger than {CmsEventLimits.MaxPayloadBytes} bytes";
    private const string PayloadNullCharReason = "payload contains \\u0000";

    private static readonly Dictionary<string, CmsEventType> Types = new(StringComparer.Ordinal)
    {
        ["publish"] = CmsEventType.Publish,
        ["unPublish"] = CmsEventType.UnPublish,
        ["delete"] = CmsEventType.Delete,
    };

    public static CmsEventValidation Validate(JsonElement element)
    {
        var rawEvent = element.GetRawText();
        if (element.ValueKind != JsonValueKind.Object)
        {
            return new FailedCmsEvent(null, rawEvent, NotAnObjectReason);
        }

        var id = ReadId(element);
        if (id is null)
        {
            return new FailedCmsEvent(null, rawEvent, InvalidIdReason);
        }

        try
        {
            return new ValidCmsEvent(ReadEvent(element, id));
        }
        catch (RuleBrokenException broken)
        {
            return new FailedCmsEvent(id, rawEvent, broken.Message);
        }
    }

    private static CmsEvent ReadEvent(JsonElement element, string id)
    {
        Require(!HasDuplicateKey(element), DuplicateKeyReason);
        var type = ReadType(element);
        var timestamp = ReadTimestamp(element);
        return type == CmsEventType.Delete
            ? new CmsEvent(id, type, Version: null, timestamp, Payload: null)
            : new CmsEvent(id, type, ReadVersion(element), timestamp, ReadPayload(element));
    }

    private static string? ReadId(JsonElement element) =>
        element.TryGetProperty(IdProperty, out var property)
        && property.ValueKind == JsonValueKind.String
        && property.GetString() is { } id
        && IsValidId(id)
            ? id
            : null;

    private static bool IsValidId(string id) =>
        id.Length is >= 1 and <= CmsEventLimits.MaxIdLength
        && !id.Any(char.IsControl)
        && !char.IsWhiteSpace(id[0])
        && !char.IsWhiteSpace(id[^1]);

    private static CmsEventType ReadType(JsonElement element)
    {
        var property = RequiredProperty(element, TypeProperty, UnknownTypeReason);
        Require(property.ValueKind == JsonValueKind.String, UnknownTypeReason);
        Require(
            Types.TryGetValue(property.GetString() ?? string.Empty, out var type),
            UnknownTypeReason
        );
        return type;
    }

    // TryGetDateTime reports Unspecified when the text has no offset; that is the case to reject.
    private static DateTimeOffset ReadTimestamp(JsonElement element)
    {
        var property = RequiredProperty(element, TimestampProperty, InvalidTimestampReason);
        Require(
            property.ValueKind == JsonValueKind.String
                && property.TryGetDateTime(out var dateTime)
                && dateTime.Kind != DateTimeKind.Unspecified
                && property.TryGetDateTimeOffset(out _),
            InvalidTimestampReason
        );
        return property.GetDateTimeOffset().ToUniversalTime();
    }

    private static long ReadVersion(JsonElement element)
    {
        var property = RequiredProperty(element, VersionProperty, InvalidVersionReason);
        Require(
            property.ValueKind == JsonValueKind.Number
                && property.TryGetInt64(out var version)
                && version >= CmsEventLimits.MinVersion,
            InvalidVersionReason
        );
        return property.GetInt64();
    }

    private static string ReadPayload(JsonElement element)
    {
        var property = RequiredProperty(element, PayloadProperty, PayloadNotObjectReason);
        Require(property.ValueKind == JsonValueKind.Object, PayloadNotObjectReason);
        var payload = property.GetRawText();
        Require(
            Encoding.UTF8.GetByteCount(payload) <= CmsEventLimits.MaxPayloadBytes,
            PayloadTooLargeReason
        );
        Require(!ContainsNullChar(property), PayloadNullCharReason);
        return payload;
    }

    private static JsonElement RequiredProperty(JsonElement element, string name, string reason)
    {
        Require(element.TryGetProperty(name, out var property), reason);
        return property;
    }

    private static bool HasDuplicateKey(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Object => ObjectHasDuplicateKey(element),
            JsonValueKind.Array => element.EnumerateArray().Any(HasDuplicateKey),
            _ => false,
        };

    private static bool ObjectHasDuplicateKey(JsonElement element)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        return element
            .EnumerateObject()
            .Any(property => !names.Add(property.Name) || HasDuplicateKey(property.Value));
    }

    // Postgres jsonb rejects \u0000 anywhere, in keys as well as values.
    private static bool ContainsNullChar(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()?.Contains('\0') ?? false,
            JsonValueKind.Object => element
                .EnumerateObject()
                .Any(property => property.Name.Contains('\0') || ContainsNullChar(property.Value)),
            JsonValueKind.Array => element.EnumerateArray().Any(ContainsNullChar),
            _ => false,
        };

    private static void Require(bool rule, string reason)
    {
        if (!rule)
        {
            throw new RuleBrokenException(reason);
        }
    }

    // Private control flow: a broken rule never leaves this class as an exception.
    private sealed class RuleBrokenException(string reason) : Exception(reason);
}
