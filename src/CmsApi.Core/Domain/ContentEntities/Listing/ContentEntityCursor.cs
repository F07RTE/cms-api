using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using CmsApi.Core.Text;

namespace CmsApi.Core.Domain.ContentEntities.Listing;

/// <summary>
/// Where a page of Content Entities ends: the <c>(updatedAt, id)</c> of its last one. Opaque and
/// unsigned: a tampered cursor that still parses only moves the start, the visibility filter still applies.
/// </summary>
public sealed record ContentEntityCursor(DateTimeOffset UpdatedAt, string Id)
{
    // Ticks come first and never hold the separator, so the id may.
    private const char Separator = ':';

    public string Encode() =>
        Base64Url.EncodeToString(
            Encoding.UTF8.GetBytes(
                $"{UpdatedAt.UtcTicks.ToString(CultureInfo.InvariantCulture)}{Separator}{Id}"
            )
        );

    public static bool TryDecode(string? token, [NotNullWhen(true)] out ContentEntityCursor? cursor)
    {
        cursor = null;
        var content = TryDecodeContent(token);
        var separatorAt = content?.IndexOf(Separator) ?? -1;
        if (content is null || separatorAt < 0 || separatorAt == content.Length - 1)
        {
            return false;
        }

        var updatedAt = TryParseTicks(content[..separatorAt]);
        if (updatedAt is null)
        {
            return false;
        }

        cursor = new ContentEntityCursor(updatedAt.Value, content[(separatorAt + 1)..]);
        return true;
    }

    private static string? TryDecodeContent(string? token) =>
        token is not null && Base64Url.IsValid(token)
            ? StrictUtf8.TryDecode(Base64Url.DecodeFromChars(token))
            : null;

    private static DateTimeOffset? TryParseTicks(string text) =>
        long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
        && ticks <= DateTimeOffset.MaxValue.UtcTicks
            ? new DateTimeOffset(ticks, TimeSpan.Zero)
            : null;
}
