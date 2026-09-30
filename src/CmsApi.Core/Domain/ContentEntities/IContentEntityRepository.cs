using CmsApi.Core.Domain.Events.Rules;

namespace CmsApi.Core.Domain.ContentEntities;

/// <summary>
/// Writes Content Entities, each under its row lock: the worker applies CMS Events, and an Admin
/// sets the local override, which CMS Events never clear.
/// </summary>
public interface IContentEntityRepository
{
    /// <summary>
    /// In one transaction: locks the Content Entity row, lets <paramref name="decide"/> work out the
    /// group from its stored state and Tombstone (each null when absent), writes the final state or
    /// Tombstone and the Event Log rows. Returns the decision once committed.
    /// </summary>
    Task<GroupDecision> ApplyGroupAsync(
        long batchId,
        string contentEntityId,
        Func<ContentEntityState?, TombstoneState?, GroupDecision> decide,
        CancellationToken cancellationToken
    );

    /// <summary>Idempotent. Null when the id is unknown or deleted.</summary>
    Task<StoredContentEntity?> DisableAsync(
        string id,
        string adminUsername,
        CancellationToken cancellationToken
    );

    /// <summary>Idempotent. Null when the id is unknown or deleted.</summary>
    Task<StoredContentEntity?> EnableAsync(string id, CancellationToken cancellationToken);
}
