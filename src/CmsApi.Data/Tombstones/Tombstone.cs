namespace CmsApi.Data.Tombstones;

public sealed class Tombstone
{
    public required string Id { get; set; }

    public DateTimeOffset DeletedAt { get; set; }

    public DateTimeOffset RecordedAt { get; set; }
}
