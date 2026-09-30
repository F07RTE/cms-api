using System.Linq.Expressions;
using CmsApi.Core.Domain.ContentEntities;

namespace CmsApi.Data.ContentEntities;

internal static class StoredContentEntityMapping
{
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

    public static readonly Func<ContentEntity, StoredContentEntity> ToStored = Projection.Compile();
}
