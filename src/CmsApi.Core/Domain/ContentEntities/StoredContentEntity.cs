namespace CmsApi.Core.Domain.ContentEntities;

/// <summary>
/// A Content Entity as stored, admin columns included. What the reads and an Admin's override
/// answer with; the API maps it to the shape the caller's role sees.
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
