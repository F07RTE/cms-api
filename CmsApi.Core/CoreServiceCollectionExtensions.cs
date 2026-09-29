using CmsApi.Core.Batches;
using CmsApi.Core.Inbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CmsApi.Core;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddCmsCore(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        // A host may register its own policy first; the default covers the rest.
        services.TryAddSingleton(new InboxRetryPolicy());
        services.AddScoped<IBatchProcessor, BatchProcessor>();
        services.AddScoped<InboxProcessor>();
        return services;
    }
}
