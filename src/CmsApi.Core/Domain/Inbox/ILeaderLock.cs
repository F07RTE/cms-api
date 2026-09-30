namespace CmsApi.Core.Domain.Inbox;

/// <summary>Makes one worker the only one processing the Inbox. Disposing lets go of the lock.</summary>
public interface ILeaderLock : IAsyncDisposable
{
    /// <summary>Takes the lock if no other worker holds it. True when this worker is leader.</summary>
    Task<bool> TryAcquireAsync(CancellationToken cancellationToken);

    /// <summary>Checks the lock is still held. False, and let go, when it was lost.</summary>
    Task<bool> IsHeldAsync(CancellationToken cancellationToken);

    /// <summary>Lets go of the lock, if held, so another worker can lead. It can be taken again later.</summary>
    Task StepDownAsync();
}
