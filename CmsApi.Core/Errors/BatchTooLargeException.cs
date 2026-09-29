using CmsApi.Core.Batches;

namespace CmsApi.Core.Errors;

public sealed class BatchTooLargeException(long bodyBytes)
    : BatchRejectedException(TooLargeMessage, SplitBatchAction, bodyBytes)
{
    private const string SplitBatchAction = "Split the CMS Events into smaller Batches.";

    private static readonly string TooLargeMessage =
        $"The body is larger than {BatchLimits.MaxBodyBytes} bytes.";
}
