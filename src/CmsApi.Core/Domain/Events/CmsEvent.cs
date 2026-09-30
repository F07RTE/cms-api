namespace CmsApi.Core.Domain.Events;

/// <summary>
/// A valid CMS Event. <see cref="Timestamp"/> is UTC. <see cref="Version"/> and
/// <see cref="Payload"/> (raw JSON object text) are null on <see cref="CmsEventType.Delete"/>.
/// </summary>
public sealed record CmsEvent(
    string Id,
    CmsEventType Type,
    long? Version,
    DateTimeOffset Timestamp,
    string? Payload
);
