using CmsApi.Core.ContentEntities;
using CmsApi.Core.Users;
using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data.ContentEntities;

public static class ContentEntityReads
{
    /// <summary>The Content Entity with this id, if <paramref name="role"/> may see it.</summary>
    public static Task<ContentEntity?> FindVisibleContentEntityAsync(
        this ReadDbContext reader,
        string id,
        UserRole role,
        CancellationToken cancellationToken
    ) =>
        reader
            .ContentEntities.Where(ContentEntityVisibility.VisibleTo<ContentEntity>(role))
            .SingleOrDefaultAsync(contentEntity => contentEntity.Id == id, cancellationToken);
}
