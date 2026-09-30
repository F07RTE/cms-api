using System.Text.Json.Serialization;
using CmsApi.Core.Domain.ContentEntities;
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
)
{
    public static AdminContentEntityResponse From(StoredContentEntity contentEntity) =>
        new(
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
