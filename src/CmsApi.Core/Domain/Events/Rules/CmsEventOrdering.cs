namespace CmsApi.Core.Domain.Events.Rules;

public static class CmsEventOrdering
{
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
