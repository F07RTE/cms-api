using System.Linq.Expressions;
using CmsApi.Core.Domain.ContentEntities;

namespace CmsApi.Data.ContentEntities;

internal static class StoredContentEntityMapping
{
    /// <summary>The mapping, as an expression, so a query can project to it in SQL.</summary>
    public static readonly Expression<Func<ContentEntity, StoredContentEntity>> Projection =
        row => new StoredContentEntity(
            row.Id,
            row.Version,
            row.Payload,
            row.LastEventAt,
            row.IsPublished,
            row.IsDisabledByAdmin,
            row.DisabledAt,
            row.DisabledBy
        );

    /// <summary>The same mapping, for a row already loaded.</summary>
    public static readonly Func<ContentEntity, StoredContentEntity> ToStored = Projection.Compile();
}
