using CmsApi.Core;
using CmsApi.Data;

namespace CmsApi.Worker;

// Public so a test can build the real host.
public static class WorkerHost
{
    // Bounds how long the current entity group gets to commit on shutdown.
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(30);

    public static HostApplicationBuilder Configure(HostApplicationBuilder builder)
    {
        builder.Services.AddCmsCore();
        builder.Services.AddCmsWriteData(builder.Configuration);
        builder
            .Services.AddOptions<WorkerOptions>()
            .Bind(builder.Configuration.GetSection(WorkerOptions.SectionName))
            .Validate(
                options => options.PollInterval > TimeSpan.Zero,
                $"{WorkerOptions.SectionName}:{nameof(WorkerOptions.PollInterval)} must be positive."
            )
            .ValidateOnStart();
        builder.Services.Configure<HostOptions>(options =>
            options.ShutdownTimeout = ShutdownTimeout
        );
        builder.Services.AddHostedService<InboxWorker>();
        return builder;
    }
}
