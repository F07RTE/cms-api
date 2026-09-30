namespace CmsApi.Core.Domain.ContentEntities.Listing;

public sealed record ContentEntityPage(
    IReadOnlyList<StoredContentEntity> Items,
    ContentEntityCursor? Next
)
{
    // The lookahead row (limit + 1) only tells that another page exists.
    public static ContentEntityPage FromRowsWithLookahead(
        IReadOnlyList<StoredContentEntity> rows,
        int limit
    )
    {
        if (rows.Count <= limit)
        {
            return new ContentEntityPage(rows, Next: null);
        }

        var items = rows.Take(limit).ToList();
        var last = items[^1];
        return new ContentEntityPage(items, new ContentEntityCursor(last.LastEventAt, last.Id));
    }
}
