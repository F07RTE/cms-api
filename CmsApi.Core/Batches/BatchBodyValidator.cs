using System.Text;
using System.Text.Json;
using CmsApi.Core.Errors;

namespace CmsApi.Core.Batches;

/// <summary>
/// Checks the whole-body rules only. Each CMS Event is validated later, by the worker.
/// </summary>
public static class BatchBodyValidator
{
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true
    );

    private static readonly JsonDocumentOptions ParseOptions = new()
    {
        MaxDepth = BatchLimits.MaxDepth,
    };

    /// <exception cref="InvalidBatchException">The body breaks a whole-body rule.</exception>
    public static ValidBatchBody Validate(ReadOnlyMemory<byte> utf8Body)
    {
        var text = Decode(utf8Body);
        using var document = Parse(utf8Body);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidBatchException("The body is not a JSON array.", utf8Body.Length);
        }

        var count = root.GetArrayLength();
        if (count is < BatchLimits.MinEvents or > BatchLimits.MaxEvents)
        {
            throw new InvalidBatchException($"The Batch has {count} CMS Events.", utf8Body.Length);
        }

        return new ValidBatchBody(text, count);
    }

    // Strict, so an invalid byte is rejected instead of silently becoming U+FFFD in the Inbox.
    private static string Decode(ReadOnlyMemory<byte> utf8Body)
    {
        try
        {
            return StrictUtf8.GetString(utf8Body.Span);
        }
        catch (DecoderFallbackException)
        {
            throw new InvalidBatchException("The body is not valid UTF-8.", utf8Body.Length);
        }
    }

    private static JsonDocument Parse(ReadOnlyMemory<byte> utf8Body)
    {
        try
        {
            return JsonDocument.Parse(utf8Body, ParseOptions);
        }
        catch (JsonException)
        {
            throw new InvalidBatchException(
                "The body is not valid JSON, or is nested too deep.",
                utf8Body.Length
            );
        }
    }
}
