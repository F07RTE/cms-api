using CmsApi.Core.Domain.ContentEntities;
using CmsApi.Core.Domain.ContentEntities.Listing;
using CmsApi.Core.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data.ContentEntities;

internal sealed class ContentEntityReadRepository(ReadDbContext reader)
    : IContentEntityReadRepository
{
    // Before every stored Content Entity, so the first page is the page after it.
    private static readonly ContentEntityCursor Start = new(DateTimeOffset.MaxValue, string.Empty);

    public Task<StoredContentEntity?> FindAsync(
        string id,
        UserRole role,
        CancellationToken cancellationToken
    ) =>
        VisibleTo(role)
            .Where(contentEntity => contentEntity.Id == id)
            .Select(StoredContentEntityMapping.Projection)
            .SingleOrDefaultAsync(cancellationToken);

    // updatedAt desc then id, as the list indexes are. The <= alone is the index condition; the
    // rest drops the rows up to and including the cursor's.
    public async Task<ContentEntityPage> ReadPageAsync(
        ContentEntityPageRequest request,
        UserRole role,
        CancellationToken cancellationToken
    )
    {
        var after = request.After ?? Start;
        var rows = await VisibleTo(role)
            .Where(contentEntity =>
                contentEntity.LastEventAt <= after.UpdatedAt
                && (
                    contentEntity.LastEventAt < after.UpdatedAt
                    || string.Compare(contentEntity.Id, after.Id) > 0
                )
            )
            .OrderByDescending(contentEntity => contentEntity.LastEventAt)
            .ThenBy(contentEntity => contentEntity.Id)
            .Take(request.Limit + 1)
            .Select(StoredContentEntityMapping.Projection)
            .ToListAsync(cancellationToken);

        return ContentEntityPage.FromRowsWithLookahead(rows, request.Limit);
    }

    // The filter is a constant per role, so each role's SQL matches its own list index.
    private IQueryable<ContentEntity> VisibleTo(UserRole role) =>
        reader.ContentEntities.Where(ContentEntityVisibility.VisibleTo<ContentEntity>(role));
}
