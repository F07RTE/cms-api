using CmsApi.Core.Domain.ContentEntities;
using CmsApi.Core.Domain.EventLog;
using CmsApi.Core.Domain.Inbox;
using CmsApi.Core.Domain.Users;
using CmsApi.Data.ContentEntities;
using CmsApi.Data.EventLog;
using CmsApi.Data.Inbox;
using CmsApi.Data.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CmsApi.Data;

public static class DataServiceCollectionExtensions
{
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
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IContentEntityReadRepository, ContentEntityReadRepository>();
        return services;
    }

    // The worker registers the writer only: it never reads from a replica.
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
        // The factory also registers WriteDbContext as scoped, for everything that uses it per scope.
        services.AddDbContextFactory<WriteDbContext>(
            (provider, options) =>
                options.UseNpgsql(ConnectionStrings(provider).Writer).UseSnakeCaseNamingConvention()
        );
        services.AddScoped<IInboxRepository, InboxRepository>();
        // Transient: each worker gets its own session, and with it its own claim on the lock.
        services.AddTransient<ILeaderLock, PgLeaderLock>();
        services.AddScoped<IEventLogRepository, EventLogRepository>();
        services.AddScoped<IContentEntityRepository, ContentEntityRepository>();
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
