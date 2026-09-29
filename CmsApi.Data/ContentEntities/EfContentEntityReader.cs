using CmsApi.Core.ContentEntities;
using CmsApi.Core.ContentEntities.Listing;

namespace CmsApi.Data.ContentEntities;

internal sealed class EfContentEntityReader<T>(
    ReadDbContext reader,
    ContentEntityQueries<T> queries
) : IContentEntityReader
    where T : class, IProjectedContentEntity
{
    // Before every stored Content Entity, so the first page is the page after it.
    private static readonly ContentEntityCursor Start = new(DateTimeOffset.MaxValue, string.Empty);

    public async Task<IProjectedContentEntity?> FindAsync(
        string id,
        CancellationToken cancellationToken
    ) => await queries.Find(reader, id, cancellationToken);

    public async Task<ContentEntityPage> ReadPageAsync(
        ContentEntityPageRequest request,
        CancellationToken cancellationToken
    )
    {
        var after = request.After ?? Start;
        // One lookahead row tells whether another page exists.
        var rows = await queries
            .PageAfter(reader, after.UpdatedAt, after.Id, request.Limit + 1)
            .ToListAsync(cancellationToken);
        return ContentEntityPage.FromRowsWithLookahead(rows, request.Limit);
    }
}
