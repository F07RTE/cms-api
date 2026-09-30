namespace CmsApi.Core.Exceptions;

public abstract class CmsApiException(string message, string action) : Exception(message)
{
    public string Action { get; } = action;
}
