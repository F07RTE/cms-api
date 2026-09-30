namespace CmsApi.Core.UseCases.ConsumeBatch;

public sealed record BatchAttempt(long BatchId, long Number, int MaxAttempts)
{
    public bool CanRetry => Number < MaxAttempts;

    public static BatchAttempt AfterFailures(long batchId, long failures, int maxAttempts) =>
        new(batchId, failures + 1, maxAttempts);
}
