using CmsApi.Core.Domain.Batches;
using CmsApi.Messaging.Consuming;
using CmsApi.Messaging.Publishing;
using CmsApi.Messaging.Topology;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CmsApi.Messaging;

public static class MessagingServiceCollectionExtensions
{
    private static readonly string[] AmqpSchemes = ["amqp", "amqps"];

    public static IServiceCollection AddCmsMessaging(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<MessagingOptions>()
            .Bind(configuration.GetSection(MessagingOptions.SectionName))
            .Configure(options =>
                options.ConnectionString =
                    configuration.GetConnectionString(MessagingOptions.ConnectionStringName)
                    ?? string.Empty
            )
            .Validate(
                options => IsAmqpUri(options.ConnectionString),
                $"{MessagingOptions.ConnectionStringsSection}:{MessagingOptions.ConnectionStringName} must be an AMQP URI."
            )
            .Validate(
                options => options.RetryDelay > TimeSpan.Zero,
                $"{MessagingOptions.SectionName}:{nameof(MessagingOptions.RetryDelay)} must be positive."
            )
            .Validate(
                options => options.MaxAttempts > 0,
                $"{MessagingOptions.SectionName}:{nameof(MessagingOptions.MaxAttempts)} must be positive."
            )
            .ValidateOnStart();
        services.AddSingleton<BrokerConnection>();
        services.AddSingleton<IBatchPublisher, BatchPublisher>();
        services.AddHostedService<BatchTopology>();
        services.AddSingleton<BatchDeliveryHandler>();
        services.AddHostedService<BatchQueueConsumer>();
        return services;
    }

    private static bool IsAmqpUri(string connectionString) =>
        Uri.TryCreate(connectionString, UriKind.Absolute, out var uri)
        && AmqpSchemes.Contains(uri.Scheme);
}
