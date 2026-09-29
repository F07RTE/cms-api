using CmsApi.Core.Inbox;
using CmsApi.Data.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CmsApi.Data;

public static class DataServiceCollectionExtensions
{
    /// <summary>Registers both contexts. For the API host.</summary>
    public static IServiceCollection AddCmsData(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddCmsWriteData(configuration);
        services
            .AddOptions<ConnectionStringOptions>()
            .Configure(options =>
                options.Reader =
                    configuration.GetConnectionString(nameof(options.Reader)) ?? string.Empty
            )
            .ValidateRequired(options => options.Reader, nameof(ConnectionStringOptions.Reader));
        services.AddDbContext<ReadDbContext>(
            (provider, options) =>
                options
                    .UseNpgsql(ConnectionStrings(provider).Reader)
                    .UseSnakeCaseNamingConvention()
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
        );
        return services;
    }

    /// <summary>Registers the writer context only. For the worker, which never reads from a replica.</summary>
    public static IServiceCollection AddCmsWriteData(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<ConnectionStringOptions>()
            .Configure(options =>
                options.Writer =
                    configuration.GetConnectionString(nameof(options.Writer)) ?? string.Empty
            )
            .ValidateRequired(options => options.Writer, nameof(ConnectionStringOptions.Writer))
            .ValidateOnStart();
        services.AddDbContext<WriteDbContext>(
            (provider, options) =>
                options.UseNpgsql(ConnectionStrings(provider).Writer).UseSnakeCaseNamingConvention()
        );
        services.AddScoped<IInbox, EfInbox>();
        return services;
    }

    private static OptionsBuilder<ConnectionStringOptions> ValidateRequired(
        this OptionsBuilder<ConnectionStringOptions> builder,
        Func<ConnectionStringOptions, string> connectionString,
        string name
    ) =>
        builder.Validate(
            options => !string.IsNullOrWhiteSpace(connectionString(options)),
            $"{ConnectionStringOptions.SectionName}:{name} is required."
        );

    private static ConnectionStringOptions ConnectionStrings(IServiceProvider provider) =>
        provider.GetRequiredService<IOptions<ConnectionStringOptions>>().Value;
}
