namespace CmsApi.Core.Domain.Events.Validation;

public static class CmsEventLimits
{
    public const int MaxIdLength = 128;
    public const long MinVersion = 1;
    public const int MaxPayloadBytes = 256 * 1024;

    // A timestamp further ahead of the server clock is logged, never rejected.
    public static readonly TimeSpan FutureSkewWarning = TimeSpan.FromMinutes(5);
}
