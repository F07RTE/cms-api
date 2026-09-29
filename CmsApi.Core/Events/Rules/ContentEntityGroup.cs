namespace CmsApi.Core.Events.Rules;

/// <summary>The CMS Events of one Batch for one Content Entity, in the order they apply.</summary>
public sealed record ContentEntityGroup(string ContentEntityId, IReadOnlyList<CmsEvent> Events);
