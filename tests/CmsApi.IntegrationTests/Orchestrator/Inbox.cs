using CmsApi.Core.Domain.Inbox;
using CmsApi.Core.UseCases.ProcessBatch;
using CmsApi.Core.UseCases.ProcessInbox;
using CmsApi.Data.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CmsApi.IntegrationTests;

/// <summary>The Inbox and the worker's processing of it, driven without waiting.</summary>
public static partial class Orchestrator
{
    public static Task<List<InboxBatch>> ReadInboxAsync() =>
        WithWriterAsync(context => context.InboxBatches.ToListAsync());

    /// <summary>Runs the worker's processing until the Inbox has nothing due. No waiting.</summary>
    public static Task DrainInboxAsync() =>
        DrainInboxAsync(provider => provider.GetRequiredService<InboxProcessor>());

    /// <summary>Drains the Inbox with <paramref name="batchProcessor"/> in place of the real one.</summary>
    public static Task DrainInboxAsync(IBatchProcessor batchProcessor) =>
        DrainInboxAsync(provider =>
            ActivatorUtilities.CreateInstance<InboxProcessor>(provider, batchProcessor)
        );

    /// <summary>Claims the next due Batch and never finishes it, as a crashed worker would.</summary>
    public static async Task OrphanNextBatchAsync()
    {
        await using var scope = CreateScope();
        var inbox = scope.ServiceProvider.GetRequiredService<IInboxRepository>();
        await inbox.ClaimNextAsync(CancellationToken.None);
    }

    /// <summary>Runs the recovery a worker runs when it becomes leader.</summary>
    public static async Task RecoverOrphansAsync()
    {
        await using var scope = CreateScope();
        var inbox = scope.ServiceProvider.GetRequiredService<IInboxRepository>();
        await inbox.RecoverOrphansAsync(CancellationToken.None);
    }

    /// <summary>Puts every Batch back to Pending, as a retry or crash recovery would.</summary>
    public static Task RequeueBatchesAsync() =>
        WithWriterAsync(context =>
            context.InboxBatches.ExecuteUpdateAsync(setters =>
                setters.SetProperty(batch => batch.Status, InboxStatus.Pending)
            )
        );

    /// <summary>A fresh leader lock, as one worker replica holds it.</summary>
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
