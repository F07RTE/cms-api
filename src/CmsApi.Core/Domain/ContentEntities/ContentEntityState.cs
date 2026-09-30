namespace CmsApi.Core.Domain.ContentEntities;

/// <summary>The CMS-owned columns of a stored Content Entity. <see cref="Payload"/> is raw JSON text.</summary>
public sealed record ContentEntityState(
    long Version,
    string Payload,
    bool IsPublished,
    DateTimeOffset LastEventAt
);
