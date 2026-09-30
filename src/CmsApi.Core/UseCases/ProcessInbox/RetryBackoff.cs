namespace CmsApi.Core.UseCases.ProcessInbox;

public static class RetryBackoff
{
    public static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(5);

    private const double Base = 2;

    public static TimeSpan After(int attempts) =>
        TimeSpan.FromSeconds(Math.Min(Math.Pow(Base, attempts), MaxDelay.TotalSeconds));
}
