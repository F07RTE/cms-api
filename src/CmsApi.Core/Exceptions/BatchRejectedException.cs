namespace CmsApi.Core.Exceptions;

public abstract class BatchRejectedException(string message, string action, long bodyBytes)
    : CmsApiException(message, action)
{
    // For the log: the body itself is never logged.
    public long BodyBytes { get; } = bodyBytes;
}
