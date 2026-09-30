namespace CmsApi.Data.Tombstones;

public sealed class Tombstone
{
    public required string Id { get; set; }

    // CMS time.
    public DateTimeOffset DeletedAt { get; set; }

    // Server time.
    public DateTimeOffset RecordedAt { get; set; }
}
