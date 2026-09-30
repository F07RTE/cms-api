namespace CmsApi.Messaging.Topology;

public static class BatchQueues
{
    public const string DefaultExchange = "";
    public const string Main = "cms.batches";
    public const string Retry = "cms.batches.retry";
}
