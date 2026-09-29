namespace CmsApi.Data.Tombstones;

/// <summary>The record that a Content Entity was deleted. Final: the id is never written again.</summary>
public sealed class Tombstone
{
    public required string Id { get; set; }

    /// <summary>CMS time of the delete.</summary>
    public DateTimeOffset DeletedAt { get; set; }

    /// <summary>Server time the worker wrote the Tombstone.</summary>
    public DateTimeOffset RecordedAt { get; set; }
}
