namespace CmsApi.Core.Exceptions;

/// <summary>An expected failure the caller can fix. <see cref="Action"/> tells them how.</summary>
public abstract class CmsApiException(string message, string action) : Exception(message)
{
    public string Action { get; } = action;
}
