namespace CmsApi.Core.Domain.Inbox;

public interface ILeaderLock : IAsyncDisposable
{
    Task<bool> TryAcquireAsync(CancellationToken cancellationToken);

    Task<bool> IsHeldAsync(CancellationToken cancellationToken);

    Task StepDownAsync();
}
