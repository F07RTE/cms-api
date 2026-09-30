using System.Text.Json;

namespace CmsApi.Core.Domain.Batches;

public static class BatchLimits
{
    public const int MaxBodyBytes = 10 * 1024 * 1024;
    public const int MinEvents = 1;
    public const int MaxEvents = 1000;

    public const int MaxDepth = 64;

    public static readonly JsonDocumentOptions ParseOptions = new() { MaxDepth = MaxDepth };
}
