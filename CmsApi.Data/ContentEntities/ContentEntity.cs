namespace CmsApi.Data.ContentEntities;

/// <summary>
/// A Content Entity row. The worker writes the CMS columns; an Admin writes the
/// <c>*DisabledBy*</c> columns. <see cref="Payload"/> is raw JSON, stored as jsonb.
/// </summary>
public sealed class ContentEntity
{
    public required string Id { get; set; }

    public long Version { get; set; }

    public required string Payload { get; set; }

    public bool IsPublished { get; set; }

    /// <summary>CMS time of the last applied CMS Event.</summary>
    public DateTimeOffset LastEventAt { get; set; }

    public bool IsDisabledByAdmin { get; set; }

    public DateTimeOffset? DisabledAt { get; set; }

    public string? DisabledBy { get; set; }
}
