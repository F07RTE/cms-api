using CmsApi.Core.Events;
using CmsApi.Core.Events.Validation;

namespace CmsApi.Core.EventLog;

public interface IEventLog
{
    /// <summary>Records a <see cref="EventOutcome.Failed"/> row, with the raw event, for each invalid CMS Event.</summary>
    Task RecordFailedAsync(
        long batchId,
        IReadOnlyList<FailedCmsEvent> failedEvents,
        CancellationToken cancellationToken
    );
}
