namespace CmsApi.Core.Domain.ContentEntities;

/// <summary>
/// A Content Entity as stored, admin columns included. What an Admin's override answers with.
/// </summary>
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
