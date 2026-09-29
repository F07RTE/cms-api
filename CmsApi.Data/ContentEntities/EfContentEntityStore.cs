using CmsApi.Core.ContentEntities;
using CmsApi.Core.Events;
using CmsApi.Core.Events.Rules;
using CmsApi.Data.EventLog;
using CmsApi.Data.Tombstones;
using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data.ContentEntities;

internal sealed class EfContentEntityStore(WriteDbContext context, TimeProvider timeProvider)
    : IContentEntityStore
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
