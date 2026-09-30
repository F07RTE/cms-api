namespace CmsApi.Core.Domain.ContentEntities;

/// <summary>The Tombstone of a deleted Content Entity. <see cref="DeletedAt"/> is the delete's CMS time.</summary>
public sealed record TombstoneState(DateTimeOffset DeletedAt);
