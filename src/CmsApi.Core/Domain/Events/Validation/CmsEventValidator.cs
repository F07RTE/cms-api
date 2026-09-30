using System.Text;
using System.Text.Json;

namespace CmsApi.Core.Domain.Events.Validation;

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

        return ReadEvent(element, id, rawEvent);
    }

    private static CmsEventValidation ReadEvent(JsonElement element, string id, string rawEvent)
    {
        if (HasDuplicateKey(element))
        {
            return new FailedCmsEvent(id, rawEvent, DuplicateKeyReason);
        }

        if (!TryReadType(element, out var type))
        {
            return new FailedCmsEvent(id, rawEvent, UnknownTypeReason);
        }

        if (!TryReadTimestamp(element, out var timestamp))
        {
            return new FailedCmsEvent(id, rawEvent, InvalidTimestampReason);
        }

        var header = new CmsEvent(id, type, Version: null, timestamp, Payload: null);
        return type == CmsEventType.Delete
            ? new ValidCmsEvent(header)
            : ReadVersioned(element, header, rawEvent);
    }

    private static CmsEventValidation ReadVersioned(
        JsonElement element,
        CmsEvent header,
        string rawEvent
    )
    {
        if (!TryReadVersion(element, out var version))
        {
            return new FailedCmsEvent(header.Id, rawEvent, InvalidVersionReason);
        }

        element.TryGetProperty(PayloadProperty, out var payload);
        return PayloadBrokenRule(payload) is { } brokenRule
            ? new FailedCmsEvent(header.Id, rawEvent, brokenRule)
            : new ValidCmsEvent(header with { Version = version, Payload = payload.GetRawText() });
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

    private static bool TryReadType(JsonElement element, out CmsEventType type)
    {
        type = default;
        return element.TryGetProperty(TypeProperty, out var property)
            && property.ValueKind == JsonValueKind.String
            && Types.TryGetValue(property.GetString() ?? string.Empty, out type);
    }

    // TryGetDateTime reports Unspecified when the text has no offset; that is the case to reject.
    private static bool TryReadTimestamp(JsonElement element, out DateTimeOffset timestamp)
    {
        timestamp = default;
        if (
            !element.TryGetProperty(TimestampProperty, out var property)
            || property.ValueKind != JsonValueKind.String
            || !property.TryGetDateTime(out var dateTime)
            || dateTime.Kind == DateTimeKind.Unspecified
            || !property.TryGetDateTimeOffset(out timestamp)
        )
        {
            return false;
        }

        timestamp = timestamp.ToUniversalTime();
        return true;
    }

    private static bool TryReadVersion(JsonElement element, out long version)
    {
        version = default;
        return element.TryGetProperty(VersionProperty, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetInt64(out version)
            && version >= CmsEventLimits.MinVersion;
    }

    // A missing payload arrives as default(JsonElement), whose kind is Undefined.
    private static string? PayloadBrokenRule(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return PayloadNotObjectReason;
        }

        if (Encoding.UTF8.GetByteCount(payload.GetRawText()) > CmsEventLimits.MaxPayloadBytes)
        {
            return PayloadTooLargeReason;
        }

        return ContainsNullChar(payload) ? PayloadNullCharReason : null;
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
}
