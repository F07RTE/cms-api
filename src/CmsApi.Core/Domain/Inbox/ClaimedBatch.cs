namespace CmsApi.Core.Domain.Inbox;

/// <summary>
/// A Batch the worker has claimed. <see cref="Body"/> is the original request text;
/// <see cref="Attempts"/> counts this claim.
/// </summary>
public sealed record ClaimedBatch(long BatchId, string Body, int Attempts);
