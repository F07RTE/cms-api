using CmsApi.Core.Events;
using CmsApi.Core.Events.Rules;

namespace CmsApi.Core.ContentEntities;

public interface IContentEntityStore
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
}
