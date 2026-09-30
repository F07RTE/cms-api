namespace CmsApi.Messaging;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";
    public const string ConnectionStringsSection = "ConnectionStrings";
    public const string ConnectionStringName = "RabbitMq";

    // An AMQP URI; it carries the broker credentials, so it's never logged.
    public string ConnectionString { get; set; } = string.Empty;

    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    public int MaxAttempts { get; set; } = 5;
}
