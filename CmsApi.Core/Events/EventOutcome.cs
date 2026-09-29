namespace CmsApi.Core.Events;

public enum EventOutcome
{
    Applied,
    SkippedStale,
    SkippedDuplicate,
    SkippedDeleted,
    SkippedUnknown,
    Failed,
}
