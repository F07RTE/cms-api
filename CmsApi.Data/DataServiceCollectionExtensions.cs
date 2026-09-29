using System.Linq.Expressions;
using CmsApi.Core.ContentEntities;
using CmsApi.Core.EventLog;
using CmsApi.Core.Inbox;
using CmsApi.Core.Users;
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
        services.AddScoped<IUserStore, EfUserStore>();
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
        // The factory also registers WriteDbContext as scoped, for everything that uses it per scope.
        services.AddDbContextFactory<WriteDbContext>(
            (provider, options) =>
                options.UseNpgsql(ConnectionStrings(provider).Writer).UseSnakeCaseNamingConvention()
        );
        services.AddScoped<IInbox, EfInbox>();
        // Transient: each worker gets its own session, and with it its own claim on the lock.
        services.AddTransient<ILeaderLock, PgLeaderLock>();
        services.AddScoped<IEventLog, EfEventLog>();
        services.AddScoped<IContentEntityStore, EfContentEntityStore>();
        return services;
    }

    /// <summary>
    /// Registers the <see cref="IContentEntityReader"/> for <paramref name="role"/>, keyed by it,
    /// projecting to <typeparamref name="T"/>. Its queries are compiled once, here.
    /// </summary>
    public static IServiceCollection AddContentEntityReader<T>(
        this IServiceCollection services,
        UserRole role,
        Expression<Func<ContentEntity, T>> projection
    )
        where T : class, IProjectedContentEntity
    {
        var queries = new ContentEntityQueries<T>(role, projection);
        services.AddKeyedScoped<IContentEntityReader>(
            role,
            (provider, _) =>
                new EfContentEntityReader<T>(provider.GetRequiredService<ReadDbContext>(), queries)
        );
        return services;
    }

    /// <summary>
    /// Registers the <see cref="IContentEntityOverrides"/>, answering with <paramref name="project"/>.
    /// </summary>
    public static IServiceCollection AddContentEntityOverrides(
        this IServiceCollection services,
        Func<ContentEntity, IProjectedContentEntity> project
    ) =>
        services.AddScoped<IContentEntityOverrides>(provider => new EfContentEntityOverrides(
            provider.GetRequiredService<WriteDbContext>(),
            provider.GetRequiredService<TimeProvider>(),
            project
        ));

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
