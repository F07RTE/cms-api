using System.Linq.Expressions;
using System.Text.Json.Serialization;
using CmsApi.Core.ContentEntities;
using CmsApi.Data.ContentEntities;
using CmsApi.Http;

namespace CmsApi.Dtos;

/// <summary>A Content Entity as an Admin sees it: the User fields plus the admin fields.</summary>
public sealed record AdminContentEntityResponse(
    string Id,
    long Version,
    [property: JsonConverter(typeof(RawJsonConverter))] string Payload,
    DateTimeOffset UpdatedAt,
    bool IsPublished,
    bool IsDisabledByAdmin,
    DateTimeOffset? DisabledAt,
    string? DisabledBy
) : IProjectedContentEntity
{
    /// <summary>The mapping, as an expression, so a query can project to this record in SQL.</summary>
    public static readonly Expression<Func<ContentEntity, AdminContentEntityResponse>> Projection =
        contentEntity => new AdminContentEntityResponse(
            contentEntity.Id,
            contentEntity.Version,
            contentEntity.Payload,
            contentEntity.LastEventAt,
            contentEntity.IsPublished,
            contentEntity.IsDisabledByAdmin,
            contentEntity.DisabledAt,
            contentEntity.DisabledBy
        );
}
