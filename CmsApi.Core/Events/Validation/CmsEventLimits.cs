namespace CmsApi.Core.Events.Validation;

/// <summary>Per-event rules, checked by <see cref="CmsEventValidator"/>.</summary>
public static class CmsEventLimits
{
    public const int MaxIdLength = 128;
    public const long MinVersion = 1;
    public const int MaxPayloadBytes = 256 * 1024;

    /// <summary>A timestamp further ahead of the server clock is logged, never rejected.</summary>
    public static readonly TimeSpan FutureSkewWarning = TimeSpan.FromMinutes(5);
}
