using CmsApi.Core.Domain.ContentEntities;
using CmsApi.Core.Domain.ContentEntities.Listing;

namespace CmsApi.Data.ContentEntities;

internal sealed class ContentEntityReadRepository<T>(
    ReadDbContext reader,
    ContentEntityQueries<T> queries
) : IContentEntityReadRepository
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

        var rows = await queries
            .PageAfter(reader, after.UpdatedAt, after.Id, request.Limit + 1)
            .ToListAsync(cancellationToken);

        return ContentEntityPage.FromRowsWithLookahead(rows, request.Limit);
    }
}
