namespace CmsApi.Core.ContentEntities;

/// <summary>
/// The Admin's local override: disables and enables Content Entities, which CMS Events never
/// clear. Each call is idempotent and returns the Content Entity as an Admin sees it.
/// </summary>
public interface IContentEntityOverrides
{
    /// <summary>Null when the id is unknown or deleted.</summary>
    Task<IProjectedContentEntity?> DisableAsync(
        string id,
        string adminUsername,
        CancellationToken cancellationToken
    );

    /// <summary>Null when the id is unknown or deleted.</summary>
    Task<IProjectedContentEntity?> EnableAsync(string id, CancellationToken cancellationToken);
}
