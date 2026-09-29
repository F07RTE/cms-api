namespace CmsApi.Core.Batches;

/// <summary>Whole-body rules, checked before a Batch reaches the Inbox.</summary>
public static class BatchLimits
{
    public const int MaxBodyBytes = 10 * 1024 * 1024;
    public const int MinEvents = 1;
    public const int MaxEvents = 1000;

    /// <summary>The System.Text.Json default, stated so it can't drift.</summary>
    public const int MaxDepth = 64;
}
