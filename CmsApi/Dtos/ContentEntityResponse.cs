using System.Text.Json.Serialization;
using CmsApi.Data.ContentEntities;
using CmsApi.Http;

namespace CmsApi.Dtos;

/// <summary>A Content Entity as a User sees it.</summary>
public sealed record ContentEntityResponse(
    string Id,
    long Version,
    [property: JsonConverter(typeof(RawJsonConverter))] string Payload,
    DateTimeOffset UpdatedAt
)
{
    public static ContentEntityResponse From(ContentEntity contentEntity) =>
        new(
            contentEntity.Id,
            contentEntity.Version,
            contentEntity.Payload,
            contentEntity.LastEventAt
        );
}
