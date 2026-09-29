using CmsApi.Core.ContentEntities;
using CmsApi.Core.Events;
using CmsApi.Core.Events.Rules;
using CmsApi.Data.EventLog;
using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data.ContentEntities;

internal sealed class EfContentEntityStore(WriteDbContext context, TimeProvider timeProvider)
    : IContentEntityStore
{
    public async Task ApplyGroupAsync(
        long batchId,
        string contentEntityId,
        Func<ContentEntityState?, GroupDecision> decide,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            cancellationToken
        );
        var row = await LockAsync(contentEntityId, cancellationToken);
        var decision = decide(row is null ? null : ToState(row));
        Write(row, contentEntityId, decision.FinalState);

        var processedAt = timeProvider.GetUtcNow();
        context.EventLog.AddRange(
            decision.DecidedEvents.Select(decided =>
                EventLogEntries.Decided(batchId, decided, processedAt)
            )
        );
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        context.ChangeTracker.Clear();
    }

    // Serialises the worker against an Admin PATCH on the same row.
    private async Task<ContentEntity?> LockAsync(string id, CancellationToken cancellationToken)
    {
        var rows = await context
            .ContentEntities.FromSql($"SELECT * FROM content_entities WHERE id = {id} FOR UPDATE")
            .ToListAsync(cancellationToken);
        return rows.SingleOrDefault();
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
