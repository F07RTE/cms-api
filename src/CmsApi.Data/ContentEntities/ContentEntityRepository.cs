using CmsApi.Core.Domain.ContentEntities;
using CmsApi.Core.Domain.Events.Rules;
using CmsApi.Data.EventLog;
using CmsApi.Data.Tombstones;
using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data.ContentEntities;

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
        var row = await LockAsync(contentEntityId, cancellationToken);
        var storedTombstone = await context.Tombstones.FindAsync(
            [contentEntityId],
            cancellationToken
        );
        var decision = decide(
            row is null ? null : new(row.Version, row.Payload, row.IsPublished, row.LastEventAt),
            storedTombstone is null ? null : new(storedTombstone.DeletedAt)
        );

        var processedAt = timeProvider.GetUtcNow();
        if (storedTombstone is null)
        {
            WriteFinal(row, contentEntityId, decision, processedAt);
        }

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

    private async Task<StoredContentEntity?> OverrideAsync(
        string id,
        Func<ContentEntity, bool> change,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            cancellationToken
        );
        var row = await LockAsync(id, cancellationToken);
        if (row is not null && change(row))
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return row is null ? null : StoredContentEntityMapping.ToStored(row);
    }

    // Row-locks until the transaction ends, so the consumer and an Admin PATCH never interleave.
    private async Task<ContentEntity?> LockAsync(string id, CancellationToken cancellationToken)
    {
        var rows = await context
            .ContentEntities.FromSql($"SELECT * FROM content_entities WHERE id = {id} FOR UPDATE")
            .ToListAsync(cancellationToken);
        return rows.SingleOrDefault();
    }

    private void WriteFinal(
        ContentEntity? row,
        string id,
        GroupDecision decision,
        DateTimeOffset processedAt
    )
    {
        if (decision.FinalTombstone is { } tombstone)
        {
            Delete(row, id, tombstone, processedAt);
        }
        else if (decision.FinalState is { } state)
        {
            Upsert(row, id, state);
        }
    }

    private void Delete(
        ContentEntity? row,
        string id,
        TombstoneState tombstone,
        DateTimeOffset processedAt
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
                DeletedAt = tombstone.DeletedAt,
                RecordedAt = processedAt,
            }
        );
    }

    // Writes the CMS columns only: the admin columns are never touched by CMS Events.
    private void Upsert(ContentEntity? row, string id, ContentEntityState state)
    {
        if (row is null)
        {
            row = new ContentEntity { Id = id, Payload = state.Payload };
            context.ContentEntities.Add(row);
        }

        row.Version = state.Version;
        row.Payload = state.Payload;
        row.IsPublished = state.IsPublished;
        row.LastEventAt = state.LastEventAt;
    }
}
