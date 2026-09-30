using CmsApi.Core.Domain.Batches;

namespace CmsApi.Core.Exceptions;

public sealed class InvalidBatchException(string message, long bodyBytes)
    : BatchRejectedException(message, FixBodyAction, bodyBytes)
{
    private static readonly string FixBodyAction =
        $"Send a UTF-8 JSON array of {BatchLimits.MinEvents} to {BatchLimits.MaxEvents} "
        + $"CMS Events, nested at most {BatchLimits.MaxDepth} levels deep.";
}
