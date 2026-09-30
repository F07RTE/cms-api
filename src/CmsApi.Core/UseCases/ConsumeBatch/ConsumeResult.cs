namespace CmsApi.Core.UseCases.ConsumeBatch;

public enum ConsumeResult
{
    Done,
    RetryLater,
    Dead,
}
