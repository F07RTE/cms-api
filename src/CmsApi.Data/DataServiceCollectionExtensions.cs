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
        services
            .AddOptions<ConnectionStringOptions>()
            .Configure(options =>
            {
                options.Reader =
                    configuration.GetConnectionString(nameof(options.Reader)) ?? string.Empty;
                options.Writer =
                    configuration.GetConnectionString(nameof(options.Writer)) ?? string.Empty;
            })
            .ValidateRequired(options => options.Reader, nameof(ConnectionStringOptions.Reader))
            .ValidateRequired(options => options.Writer, nameof(ConnectionStringOptions.Writer))
            .ValidateOnStart();
        services.AddDbContext<ReadDbContext>(
            (provider, options) =>
                options
                    .UseNpgsql(ConnectionStrings(provider).Reader)
                    .UseSnakeCaseNamingConvention()
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
        );
        // The factory also registers WriteDbContext as scoped, for everything that uses it per scope.
        services.AddDbContextFactory<WriteDbContext>(
            (provider, options) =>
                options.UseNpgsql(ConnectionStrings(provider).Writer).UseSnakeCaseNamingConvention()
        );
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IContentEntityReadRepository, ContentEntityReadRepository>();
        services.AddScoped<IInboxRepository, InboxRepository>();
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
