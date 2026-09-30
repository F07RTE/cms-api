namespace CmsApi.Core.Domain.Events;

public enum EventOutcome
{
    Applied,
    SkippedStale,
    SkippedDuplicate,
    SkippedDeleted,
    SkippedUnknown,
    Failed,
}
