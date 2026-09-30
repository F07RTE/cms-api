using CmsApi.Core.Domain.Inbox;
using CmsApi.Core.UseCases.ProcessBatch;
using CmsApi.Core.UseCases.ProcessInbox;
using CmsApi.Data.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CmsApi.IntegrationTests;

public static partial class Orchestrator
{
    public static Task<List<InboxBatch>> ReadInboxAsync() =>
        WithWriterAsync(context => context.InboxBatches.ToListAsync());

    public static Task DrainInboxAsync() =>
        DrainInboxAsync(provider => provider.GetRequiredService<InboxProcessor>());

    public static Task DrainInboxAsync(IBatchProcessor batchProcessor) =>
        DrainInboxAsync(provider =>
            ActivatorUtilities.CreateInstance<InboxProcessor>(provider, batchProcessor)
        );

    public static async Task OrphanNextBatchAsync()
    {
        await using var scope = CreateScope();
        var inbox = scope.ServiceProvider.GetRequiredService<IInboxRepository>();
        await inbox.ClaimNextAsync(CancellationToken.None);
    }

    public static async Task RecoverOrphansAsync()
    {
        await using var scope = CreateScope();
        var inbox = scope.ServiceProvider.GetRequiredService<IInboxRepository>();
        await inbox.RecoverOrphansAsync(CancellationToken.None);
    }

    public static Task RequeueBatchesAsync() =>
        WithWriterAsync(context =>
            context.InboxBatches.ExecuteUpdateAsync(setters =>
                setters.SetProperty(batch => batch.Status, InboxStatus.Pending)
            )
        );

    public static ILeaderLock CreateLeaderLock() =>
        Factory.Services.GetRequiredService<ILeaderLock>();

    // A scope per Batch, as the worker has.
    private static async Task DrainInboxAsync(
        Func<IServiceProvider, InboxProcessor> createProcessor
    )
    {
        bool processed;
        do
        {
            await using var scope = CreateScope();
            var processor = createProcessor(scope.ServiceProvider);
            processed = await processor.ProcessNextBatchAsync(CancellationToken.None);
        } while (processed);
    }
}
