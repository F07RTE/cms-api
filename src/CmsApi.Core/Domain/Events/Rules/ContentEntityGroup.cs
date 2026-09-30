namespace CmsApi.Core.Domain.Events.Rules;

public sealed record ContentEntityGroup(string ContentEntityId, IReadOnlyList<CmsEvent> Events);
