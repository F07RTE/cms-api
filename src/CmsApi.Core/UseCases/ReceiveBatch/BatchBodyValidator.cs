using System.Text.Json;
using CmsApi.Core.Domain.Batches;
using CmsApi.Core.Exceptions;
using CmsApi.Core.Text;

namespace CmsApi.Core.UseCases.ReceiveBatch;

// Whole-body rules only: each CMS Event is validated later, by the worker.
public static class BatchBodyValidator
{
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

    private static string Decode(ReadOnlyMemory<byte> utf8Body) =>
        StrictUtf8.TryDecode(utf8Body.Span)
        ?? throw new InvalidBatchException("The body is not valid UTF-8.", utf8Body.Length);

    private static JsonDocument Parse(ReadOnlyMemory<byte> utf8Body)
    {
        try
        {
            return JsonDocument.Parse(utf8Body, BatchLimits.ParseOptions);
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
