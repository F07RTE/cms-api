namespace CmsApi.Core.Domain.ContentEntities;

public sealed record ContentEntityState(
    long Version,
    string Payload,
    bool IsPublished,
    DateTimeOffset LastEventAt
);
