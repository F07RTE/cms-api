namespace CmsApi.Core.ContentEntities.Listing;

/// <summary>One page of Content Entities. <see cref="Next"/> is null on the last page.</summary>
public sealed record ContentEntityPage(
    IReadOnlyList<IProjectedContentEntity> Items,
    ContentEntityCursor? Next
)
{
    /// <summary>
    /// Builds the page from up to <paramref name="limit"/> + 1 rows: the lookahead row, if there,
    /// only tells that another page exists.
    /// </summary>
    public static ContentEntityPage FromRowsWithLookahead(
        IReadOnlyList<IProjectedContentEntity> rows,
        int limit
    )
    {
        if (rows.Count <= limit)
        {
            return new ContentEntityPage(rows, Next: null);
        }

        var items = rows.Take(limit).ToList();
        var last = items[^1];
        return new ContentEntityPage(items, new ContentEntityCursor(last.UpdatedAt, last.Id));
    }
}
