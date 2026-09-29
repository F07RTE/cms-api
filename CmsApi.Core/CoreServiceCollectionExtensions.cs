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
        services.AddScoped<IBatchProcessor, BatchProcessor>();
        services.AddScoped<InboxProcessor>();
        return services;
    }
}
