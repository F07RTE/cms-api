namespace CmsApi.Core.Domain.ContentEntities;

public sealed record TombstoneState(DateTimeOffset DeletedAt);
