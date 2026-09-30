using CmsApi.Core.Domain.ContentEntities.Listing;

namespace CmsApi.Core.Domain.ContentEntities;

/// <summary>
/// Reads the Content Entities one role may see, already projected to that role's shape. One
/// implementation per role, keyed by <see cref="Users.UserRole"/>.
/// </summary>
public interface IContentEntityReadRepository
{
    /// <summary>Null when the id is unknown, deleted or hidden from this role.</summary>
    Task<IProjectedContentEntity?> FindAsync(string id, CancellationToken cancellationToken);

    Task<ContentEntityPage> ReadPageAsync(
        ContentEntityPageRequest request,
        CancellationToken cancellationToken
    );
}
