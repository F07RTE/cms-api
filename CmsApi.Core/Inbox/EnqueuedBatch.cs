namespace CmsApi.Core.Inbox;

public sealed record EnqueuedBatch(long BatchId, int EventCount, DateTimeOffset ReceivedAt);
