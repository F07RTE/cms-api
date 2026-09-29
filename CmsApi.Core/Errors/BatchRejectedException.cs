namespace CmsApi.Core.Errors;

/// <summary>A Batch that broke a whole-body rule and never reached the Inbox.</summary>
public abstract class BatchRejectedException(string message, string action, long bodyBytes)
    : CmsApiException(message, action)
{
    /// <summary>Bytes received, for the log. The body itself is never logged.</summary>
    public long BodyBytes { get; } = bodyBytes;
}
