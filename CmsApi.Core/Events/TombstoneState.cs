namespace CmsApi.Core.Events;

/// <summary>The Tombstone of a deleted Content Entity. <see cref="DeletedAt"/> is the delete's CMS time.</summary>
public sealed record TombstoneState(DateTimeOffset DeletedAt);
