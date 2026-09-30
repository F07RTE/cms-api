using CmsApi.Core.Domain.Inbox;
using CmsApi.Core.UseCases.ProcessBatch;
using CmsApi.Data.Inbox;
using CmsApi.Messaging.Consuming;
using CmsApi.Messaging.Topology;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CmsApi.IntegrationTests;

public static partial class Orchestrator
{
    public static Task<List<InboxBatch>> ReadInboxAsync() =>
        WithWriterAsync(context => context.InboxBatches.ToListAsync());

    public static Task DrainInboxAsync() => DrainInboxAsync(Factory.Services);

    public static Task DrainInboxAsync(IBatchProcessor batchProcessor) =>
        DrainInboxWithAsync(services => services.AddScoped(_ => batchProcessor));

    public static Task DrainInboxAsync(IBatchProcessor batchProcessor, IInboxRepository inbox) =>
        DrainInboxWithAsync(services =>
            services.AddScoped(_ => batchProcessor).AddScoped(_ => inbox)
        );

    public static Task RequeueBatchesAsync() =>
        WithWriterAsync(context =>
            context.InboxBatches.ExecuteUpdateAsync(setters =>
                setters.SetProperty(batch => batch.Status, InboxStatus.Pending)
            )
        );

    private static async Task DrainInboxWithAsync(Action<IServiceCollection> replaceServices)
    {
        await using var factory = Factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(replaceServices)
        );
        await DrainInboxAsync(factory.Services);
    }

    // Runs the consumer's handler on every message BasicGet finds, as the consumer would.
    private static async Task DrainInboxAsync(IServiceProvider services)
    {
        var handler = services.GetRequiredService<BatchDeliveryHandler>();
        await using var channel = await CreateChannelAsync(services);
        while (await channel.BasicGetAsync(BatchQueues.Main, autoAck: false) is { } message)
        {
            await handler.HandleAsync(
                new BatchDelivery(
                    channel,
                    message.DeliveryTag,
                    message.BasicProperties,
                    message.Body
                ),
                CancellationToken.None
            );
        }
    }
}
