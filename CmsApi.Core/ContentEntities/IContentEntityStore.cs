using CmsApi.Core.Events;
using CmsApi.Core.Events.Rules;

namespace CmsApi.Core.ContentEntities;

public interface IContentEntityStore
{
    /// <summary>
    /// In one transaction: locks the Content Entity row, lets <paramref name="decide"/> work out the
    /// group from its stored state (null when unknown), writes the final state and the Event Log rows.
    /// </summary>
    Task ApplyGroupAsync(
        long batchId,
        string contentEntityId,
        Func<ContentEntityState?, GroupDecision> decide,
        CancellationToken cancellationToken
    );
}
