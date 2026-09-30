namespace CmsApi.Core.Domain.ContentEntities;

/// <summary>
/// A Content Entity projected to what one role sees. Exposes the <c>(updatedAt, id)</c> a page
/// cursor is made of.
/// </summary>
public interface IProjectedContentEntity
{
    string Id { get; }

    DateTimeOffset UpdatedAt { get; }
}
