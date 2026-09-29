namespace CmsApi.Core.Inbox;

/// <summary>A Batch the worker has claimed. <see cref="Body"/> is the original request text.</summary>
public sealed record ClaimedBatch(long BatchId, string Body);
