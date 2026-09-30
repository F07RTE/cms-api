namespace CmsApi.Core.Exceptions;

public abstract class BatchRejectedException(string message, string action, long bodyBytes)
    : CmsApiException(message, action)
{
    public long BodyBytes { get; } = bodyBytes;
}
