using CmsApi.Core.Domain.Events.Rules;

namespace CmsApi.Core.Domain.ContentEntities;

public interface IContentEntityRepository
{
    // One transaction under the row lock: decide runs on the locked state, then its result and the Event Log rows are written.
    Task<GroupDecision> ApplyGroupAsync(
        long batchId,
        string contentEntityId,
        Func<ContentEntityState?, TombstoneState?, GroupDecision> decide,
        CancellationToken cancellationToken
    );

    Task<StoredContentEntity?> DisableAsync(
        string id,
        string adminUsername,
        CancellationToken cancellationToken
    );

    Task<StoredContentEntity?> EnableAsync(string id, CancellationToken cancellationToken);
}
