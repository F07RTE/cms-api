namespace CmsApi.Core.Domain.Events.Validation;

public static class CmsEventLimits
{
    public const int MaxIdLength = 128;
    public const long MinVersion = 1;
    public const int MaxPayloadBytes = 256 * 1024;

    public static readonly TimeSpan FutureSkewWarning = TimeSpan.FromMinutes(5);
}
