using CmsApi.Core.Inbox;
using Microsoft.Extensions.Options;

namespace CmsApi.Worker;

/// <summary>Drains the Inbox back to back, and sleeps for the poll interval when it is empty.</summary>
internal sealed class InboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<WorkerOptions> options,
    TimeProvider timeProvider
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!await ProcessNextBatchAsync(stoppingToken))
            {
                await Task.Delay(options.Value.PollInterval, timeProvider, stoppingToken);
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
}
