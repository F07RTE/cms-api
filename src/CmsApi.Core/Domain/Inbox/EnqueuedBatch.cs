namespace CmsApi.Core.Domain.Inbox;

public sealed record EnqueuedBatch(long BatchId, int EventCount, DateTimeOffset ReceivedAt);
