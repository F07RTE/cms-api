using CmsApi.Core.Inbox;
using Microsoft.Extensions.Options;

namespace CmsApi.Worker;

/// <summary>
/// Leads the Inbox while it holds the leader lock: recovers orphans, then drains back to back and
/// sleeps for the poll interval when nothing is due. Without the lock it idles and retries.
/// </summary>
internal sealed class InboxWorker(
    IServiceScopeFactory scopeFactory,
    ILeaderLock leaderLock,
    IOptions<WorkerOptions> options,
    TimeProvider timeProvider,
    ILogger<InboxWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await LeadIfAcquiredAsync(stoppingToken);
            }
            // An infrastructure failure outside a Batch: step down and start over.
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Inbox worker failed; stepping down to retry");
                await leaderLock.StepDownAsync();
            }

            await SleepAsync(stoppingToken);
        }
    }

    private async Task LeadIfAcquiredAsync(CancellationToken stoppingToken)
    {
        if (!await leaderLock.TryAcquireAsync(stoppingToken))
        {
            logger.LogDebug("Another worker holds the leader lock; idling");
            return;
        }

        logger.LogInformation("Worker is leader; recovering orphaned Batches");
        await InScopeAsync(processor => processor.RecoverOrphansAsync(stoppingToken));
        await DrainWhileLeaderAsync(stoppingToken);
        logger.LogWarning("Worker lost the leader lock; stepping down");
    }

    // The lock is re-checked before every claim, so a worker that lost it stops claiming.
    private async Task DrainWhileLeaderAsync(CancellationToken stoppingToken)
    {
        while (await leaderLock.IsHeldAsync(stoppingToken))
        {
            var processed = await InScopeAsync(processor =>
                processor.ProcessNextBatchAsync(stoppingToken)
            );
            if (!processed)
            {
                await SleepAsync(stoppingToken);
            }
        }
    }

    private Task SleepAsync(CancellationToken stoppingToken) =>
        Task.Delay(options.Value.PollInterval, timeProvider, stoppingToken);

    // A scope per call, so each Batch gets a fresh DbContext.
    private async Task<T> InScopeAsync<T>(Func<InboxProcessor, Task<T>> action)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<InboxProcessor>());
    }

    private Task InScopeAsync(Func<InboxProcessor, Task> action) =>
        InScopeAsync(async processor =>
        {
            await action(processor);
            return true;
        });
}
