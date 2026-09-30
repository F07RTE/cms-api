using CmsApi.Core.Domain.ContentEntities.Listing;
using CmsApi.Core.Domain.Users;

namespace CmsApi.Core.Domain.ContentEntities;

public interface IContentEntityReadRepository
{
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
