namespace CmsApi.Core.Domain.ContentEntities;

public sealed record StoredContentEntity(
    string Id,
    long Version,
    string Payload,
    DateTimeOffset LastEventAt,
    bool IsPublished,
    bool IsDisabledByAdmin,
    DateTimeOffset? DisabledAt,
    string? DisabledBy
);
