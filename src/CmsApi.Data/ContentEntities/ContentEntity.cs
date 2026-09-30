namespace CmsApi.Data.ContentEntities;

public sealed class ContentEntity
{
    public required string Id { get; set; }

    public long Version { get; set; }

    public required string Payload { get; set; }

    public bool IsPublished { get; set; }

    public DateTimeOffset LastEventAt { get; set; }

    public bool IsDisabledByAdmin { get; set; }

    public DateTimeOffset? DisabledAt { get; set; }

    public string? DisabledBy { get; set; }

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
