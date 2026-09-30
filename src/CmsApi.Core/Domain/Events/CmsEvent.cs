namespace CmsApi.Core.Domain.Events;

public sealed record CmsEvent(
    string Id,
    CmsEventType Type,
    long? Version,
    DateTimeOffset Timestamp,
    string? Payload
);
