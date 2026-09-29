namespace CmsApi.Core.Events.Rules;

public static class CmsEventOrdering
{
    /// <summary>Groups by Content Entity id, each group sorted by timestamp, then version.</summary>
    public static IReadOnlyList<ContentEntityGroup> GroupById(IEnumerable<CmsEvent> events) =>
        events
            .GroupBy(cmsEvent => cmsEvent.Id, StringComparer.Ordinal)
            .Select(group => new ContentEntityGroup(
                group.Key,
                [
                    .. group
                        .OrderBy(cmsEvent => cmsEvent.Timestamp)
                        .ThenBy(cmsEvent => cmsEvent.Version),
                ]
            ))
            .ToList();
}
