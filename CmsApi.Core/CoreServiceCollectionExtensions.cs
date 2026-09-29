using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CmsApi.Core;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddCmsCore(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
