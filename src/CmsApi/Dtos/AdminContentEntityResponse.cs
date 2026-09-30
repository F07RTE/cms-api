using System.Text.Json.Serialization;
using CmsApi.Core.Domain.ContentEntities;
using CmsApi.Http;

namespace CmsApi.Dtos;

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
