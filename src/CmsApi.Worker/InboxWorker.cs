using CmsApi.Core.Domain.Inbox;
using CmsApi.Core.UseCases.ProcessInbox;
using Microsoft.Extensions.Options;

namespace CmsApi.Worker;

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
            logger.LogDebug("Another worker is Leader; idling");
            return;
        }

        logger.LogInformation("Worker is Leader; recovering Orphaned Batches");
        await RecoverOrphansAsync(stoppingToken);
        await DrainWhileLeaderAsync(stoppingToken);
        logger.LogWarning("Worker is no longer Leader; stepping down");
    }

    private async Task RecoverOrphansAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var inbox = scope.ServiceProvider.GetRequiredService<IInboxRepository>();
        await inbox.RecoverOrphansAsync(stoppingToken);
    }

    // The lock is re-checked before every claim, so a worker that lost it stops claiming.
    private async Task DrainWhileLeaderAsync(CancellationToken stoppingToken)
    {
        while (await leaderLock.IsHeldAsync(stoppingToken))
        {
            if (!await ProcessNextBatchAsync(stoppingToken))
            {
                await SleepAsync(stoppingToken);
            }
        }
    }

    // A scope per Batch, so each Batch gets a fresh DbContext.
    private async Task<bool> ProcessNextBatchAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<InboxProcessor>();
        return await processor.ProcessNextBatchAsync(stoppingToken);
    }

    private Task SleepAsync(CancellationToken stoppingToken) =>
        Task.Delay(options.Value.PollInterval, timeProvider, stoppingToken);
}
