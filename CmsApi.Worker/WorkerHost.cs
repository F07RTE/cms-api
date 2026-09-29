using CmsApi.Core;
using CmsApi.Data;

namespace CmsApi.Worker;

/// <summary>Builds the worker host. Public so a test can build the real host.</summary>
public static class WorkerHost
{
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
        builder.Services.AddHostedService<InboxWorker>();
        return builder;
    }
}
