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

    /// <summary>
    /// Marks it Disabled by <paramref name="adminUsername"/>. False, changing nothing, when it
    /// already was: the earlier <see cref="DisabledAt"/> and <see cref="DisabledBy"/> stay.
    /// </summary>
    public bool Disable(string adminUsername, DateTimeOffset disabledAt)
    {
        if (IsDisabledByAdmin)
        {
            return false;
        }

        IsDisabledByAdmin = true;
        DisabledAt = disabledAt;
        DisabledBy = adminUsername;
        return true;
    }

    /// <summary>Clears the Disabled override. False, changing nothing, when it wasn't Disabled.</summary>
    public bool Enable()
    {
        if (!IsDisabledByAdmin)
        {
            return false;
        }

        IsDisabledByAdmin = false;
        DisabledAt = null;
        DisabledBy = null;
        return true;
    }
}
