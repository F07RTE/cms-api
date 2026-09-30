using System.Globalization;
using CmsApi.Core.Exceptions;

namespace CmsApi.Core.Domain.ContentEntities.Listing;

/// <summary>The <c>limit</c> and <c>cursor</c> of a <c>GET /entities</c>, parsed.</summary>
public sealed record ContentEntityPageRequest(int Limit, ContentEntityCursor? After)
{
    /// <exception cref="InvalidPageRequestException">Either value doesn't parse.</exception>
    public static ContentEntityPageRequest Parse(string? limit, string? cursor) =>
        new(ParseLimit(limit), ParseCursor(cursor));

    private static int ParseLimit(string? limit)
    {
        if (limit is null)
        {
            return PageLimits.Default;
        }

        return
            int.TryParse(limit, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            && parsed is >= PageLimits.Min and <= PageLimits.Max
            ? parsed
            : throw InvalidPageRequestException.Limit();
    }

    private static ContentEntityCursor? ParseCursor(string? cursor)
    {
        if (cursor is null)
        {
            return null;
        }

        return ContentEntityCursor.TryDecode(cursor, out var parsed)
            ? parsed
            : throw InvalidPageRequestException.Cursor();
    }
}
