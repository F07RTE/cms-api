using CmsApi.Core;
using CmsApi.Core.Inbox;
using CmsApi.Data;
using Microsoft.Extensions.Options;

namespace CmsApi.Worker;

/// <summary>Builds the worker host. Public so a test can build the real host.</summary>
public static class WorkerHost
{
    // Bounds how long the current entity group gets to commit on shutdown.
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(30);

    public static HostApplicationBuilder Configure(HostApplicationBuilder builder)
    {
        // Before AddCmsCore, whose default policy only fills the gap for the API host.
        builder.Services.AddSingleton(provider => new InboxRetryPolicy(
            provider.GetRequiredService<IOptions<WorkerOptions>>().Value.MaxAttempts
        ));
        builder.Services.AddCmsCore();
        builder.Services.AddCmsWriteData(builder.Configuration);
        builder
            .Services.AddOptions<WorkerOptions>()
            .Bind(builder.Configuration.GetSection(WorkerOptions.SectionName))
            .Validate(
                options => options.PollInterval > TimeSpan.Zero,
                $"{WorkerOptions.SectionName}:{nameof(WorkerOptions.PollInterval)} must be positive."
            )
            .Validate(
                options => options.MaxAttempts > 0,
                $"{WorkerOptions.SectionName}:{nameof(WorkerOptions.MaxAttempts)} must be positive."
            )
            .ValidateOnStart();
        builder.Services.Configure<HostOptions>(options =>
            options.ShutdownTimeout = ShutdownTimeout
        );
        builder.Services.AddHostedService<InboxWorker>();
        return builder;
    }
}
