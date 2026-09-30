using CmsApi.Core.Domain.Events;
using CmsApi.Core.Domain.Events.Validation;

namespace CmsApi.Core.Domain.EventLog;

public interface IEventLogRepository
{
    /// <summary>
    /// Records a <see cref="EventOutcome.Failed"/> row, with the raw event, for each invalid CMS Event.
    /// False when there was nothing new to record: no invalid CMS Events, or a replay of a Batch
    /// that has its rows already.
    /// </summary>
    Task<bool> RecordFailedAsync(
        long batchId,
        IReadOnlyList<FailedCmsEvent> failedEvents,
        CancellationToken cancellationToken
    );
}
