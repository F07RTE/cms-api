using CmsApi.Core.Domain.ContentEntities;
using CmsApi.Core.Domain.Events.Rules;
using CmsApi.Data.EventLog;
using CmsApi.Data.Tombstones;

namespace CmsApi.Data.ContentEntities;

/// <summary>
/// The writer's side of <c>content_entities</c>. Every write takes the row lock inside its
/// transaction; a group also writes <c>tombstones</c> and <c>event_log</c> in that transaction.
/// </summary>
internal sealed class ContentEntityRepository(WriteDbContext context, TimeProvider timeProvider)
    : IContentEntityRepository
{
    public async Task<GroupDecision> ApplyGroupAsync(
        long batchId,
        string contentEntityId,
        Func<ContentEntityState?, TombstoneState?, GroupDecision> decide,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            cancellationToken
        );

        var row = await context.LockContentEntityAsync(contentEntityId, cancellationToken);
        var storedTombstone = await FindTombstoneAsync(contentEntityId, cancellationToken);
        var decision = decide(row is null ? null : ToState(row), storedTombstone);

        var processedAt = timeProvider.GetUtcNow();

        WriteFinal(row, storedTombstone, contentEntityId, decision, processedAt);
        context.EventLog.AddRange(
            decision.DecidedEvents.Select(decided =>
                EventLogEntries.Decided(batchId, decided, processedAt)
            )
        );

        await context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        context.ChangeTracker.Clear();

        return decision;
    }

    public Task<StoredContentEntity?> DisableAsync(
        string id,
        string adminUsername,
        CancellationToken cancellationToken
    ) =>
        OverrideAsync(
            id,
            row => row.Disable(adminUsername, timeProvider.GetUtcNow()),
            cancellationToken
        );

    public Task<StoredContentEntity?> EnableAsync(string id, CancellationToken cancellationToken) =>
        OverrideAsync(id, row => row.Enable(), cancellationToken);

    // Writes the admin columns only. Already in the target state: change returns false and nothing
    // is written. The answer is the locked row, so it comes from the writer.
    private async Task<StoredContentEntity?> OverrideAsync(
        string id,
        Func<ContentEntity, bool> change,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            cancellationToken
        );
        var row = await context.LockContentEntityAsync(id, cancellationToken);
        if (row is not null && change(row))
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return row is null ? null : StoredContentEntityMapping.ToStored(row);
    }

    private async Task<TombstoneState?> FindTombstoneAsync(
        string id,
        CancellationToken cancellationToken
    )
    {
        var tombstone = await context.Tombstones.FindAsync([id], cancellationToken);
        return tombstone is null ? null : new TombstoneState(tombstone.DeletedAt);
    }

    // An existing Tombstone is final: the group changed nothing.
    private void WriteFinal(
        ContentEntity? row,
        TombstoneState? storedTombstone,
        string id,
        GroupDecision decision,
        DateTimeOffset processedAt
    )
    {
        if (storedTombstone is not null)
        {
            return;
        }

        if (decision.FinalTombstone is null)
        {
            Write(row, id, decision.FinalState);
        }
        else
        {
            Delete(row, id, decision.FinalTombstone, processedAt);
        }
    }

    // Hard delete: the row goes, the Tombstone takes its place in the same transaction.
    private void Delete(
        ContentEntity? row,
        string id,
        TombstoneState newTombstone,
        DateTimeOffset recordedAt
    )
    {
        if (row is not null)
        {
            context.ContentEntities.Remove(row);
        }

        context.Tombstones.Add(
            new Tombstone
            {
                Id = id,
                DeletedAt = newTombstone.DeletedAt,
                RecordedAt = recordedAt,
            }
        );
    }

    private void Write(ContentEntity? row, string id, ContentEntityState? state)
    {
        if (state is null)
        {
            return;
        }

        if (row is null)
        {
            row = new ContentEntity { Id = id, Payload = state.Payload };
            context.ContentEntities.Add(row);
        }

        ApplyCmsColumns(row, state);
    }

    // The admin columns are never touched by CMS Events.
    private static void ApplyCmsColumns(ContentEntity row, ContentEntityState state)
    {
        row.Version = state.Version;
        row.Payload = state.Payload;
        row.IsPublished = state.IsPublished;
        row.LastEventAt = state.LastEventAt;
    }

    private static ContentEntityState ToState(ContentEntity row) =>
        new(row.Version, row.Payload, row.IsPublished, row.LastEventAt);
}
