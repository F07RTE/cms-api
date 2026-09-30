using CmsApi.Core.Domain.ContentEntities.Listing;
using CmsApi.Core.Domain.Users;

namespace CmsApi.Core.Domain.ContentEntities;

/// <summary>Reads the Content Entities a role may see: a User only Visible ones, an Admin all.</summary>
public interface IContentEntityReadRepository
{
    /// <summary>Null when the id is unknown, deleted or hidden from this role.</summary>
    Task<StoredContentEntity?> FindAsync(
        string id,
        UserRole role,
        CancellationToken cancellationToken
    );

    Task<ContentEntityPage> ReadPageAsync(
        ContentEntityPageRequest request,
        UserRole role,
        CancellationToken cancellationToken
    );
}
