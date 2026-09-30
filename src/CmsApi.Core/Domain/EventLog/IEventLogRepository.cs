using CmsApi.Core.Domain.Events;
using CmsApi.Core.Domain.Events.Validation;

namespace CmsApi.Core.Domain.EventLog;

public interface IEventLogRepository
{
    Task<bool> RecordFailedAsync(
        long batchId,
        IReadOnlyList<FailedCmsEvent> failedEvents,
        CancellationToken cancellationToken
    );
}
