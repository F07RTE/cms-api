using System.Linq.Expressions;
using System.Text.Json.Serialization;
using CmsApi.Core.Domain.ContentEntities;
using CmsApi.Data.ContentEntities;
using CmsApi.Http;

namespace CmsApi.Dtos;

/// <summary>A Content Entity as a User sees it.</summary>
public sealed record ContentEntityResponse(
    string Id,
    long Version,
    [property: JsonConverter(typeof(RawJsonConverter))] string Payload,
    DateTimeOffset UpdatedAt
) : IProjectedContentEntity
{
    /// <summary>The mapping, as an expression, so a query can project to this record in SQL.</summary>
    public static readonly Expression<Func<ContentEntity, ContentEntityResponse>> Projection =
        contentEntity => new ContentEntityResponse(
            contentEntity.Id,
            contentEntity.Version,
            contentEntity.Payload,
            contentEntity.LastEventAt
        );
}
