using System.Text.Json.Serialization;
using CmsApi.Core.Domain.ContentEntities;
using CmsApi.Http;

namespace CmsApi.Dtos;

public sealed record ContentEntityResponse(
    string Id,
    long Version,
    [property: JsonConverter(typeof(RawJsonConverter))] string Payload,
    DateTimeOffset UpdatedAt
)
{
    public static ContentEntityResponse From(StoredContentEntity contentEntity) =>
        new(
            contentEntity.Id,
            contentEntity.Version,
            contentEntity.Payload,
            contentEntity.LastEventAt
        );
}
