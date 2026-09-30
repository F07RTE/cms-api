using CmsApi.Core.Domain.ContentEntities.Listing;

namespace CmsApi.Core.Exceptions;

public sealed class InvalidPageRequestException(string message, string action)
    : CmsApiException(message, action)
{
    private const string CursorMessage = "The cursor is not one this API issued.";
    private const string CursorAction =
        "Pass the nextCursor of the previous page as-is, or drop it to start from the top.";

    private static readonly string LimitRange = $"from {PageLimits.Min} to {PageLimits.Max}";
    private static readonly string LimitMessage = $"The limit must be a whole number {LimitRange}.";
    private static readonly string LimitAction =
        $"Send a limit {LimitRange}, or none for {PageLimits.Default}.";

    public static InvalidPageRequestException Limit() => new(LimitMessage, LimitAction);

    public static InvalidPageRequestException Cursor() => new(CursorMessage, CursorAction);
}
