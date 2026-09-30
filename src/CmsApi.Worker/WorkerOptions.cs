namespace CmsApi.Worker;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);
}
