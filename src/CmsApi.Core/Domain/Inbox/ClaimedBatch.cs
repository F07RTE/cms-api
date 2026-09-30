namespace CmsApi.Core.Domain.Inbox;

public sealed record ClaimedBatch(long BatchId, string Body, int Attempts);
