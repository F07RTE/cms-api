using CmsApi.Core.UseCases.ConsumeBatch;
using CmsApi.Core.UseCases.ProcessBatch;
using CmsApi.Core.UseCases.ReceiveBatch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CmsApi.Core;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddCmsCore(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<BatchReceiver>();
        services.AddScoped<BatchOutcomeLog>();
        services.AddScoped<IBatchProcessor, BatchProcessor>();
        services.AddScoped<BatchConsumer>();
        return services;
    }
}
