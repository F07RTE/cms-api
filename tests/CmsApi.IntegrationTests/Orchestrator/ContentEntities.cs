using CmsApi.Data.ContentEntities;
using CmsApi.Data.EventLog;
using CmsApi.Data.Tombstones;
using Microsoft.EntityFrameworkCore;

namespace CmsApi.IntegrationTests;

public static partial class Orchestrator
{
    public static ContentEntity NewContentEntity(
        string id,
        DateTimeOffset lastEventAt,
        bool isPublished,
        long version = 1,
        string payload = "{}"
    ) =>
        new()
        {
            Id = id,
            Version = version,
            Payload = payload,
            IsPublished = isPublished,
            LastEventAt = lastEventAt,
        };

    public static ContentEntity Disable(
        ContentEntity contentEntity,
        string adminUsername,
        DateTimeOffset disabledAt
    )
    {
        contentEntity.Disable(adminUsername, disabledAt);
        return contentEntity;
    }

    public static Task<ContentEntity> SeedEntityAsync(ContentEntity contentEntity) =>
        WithWriterAsync(async context =>
        {
            context.ContentEntities.Add(contentEntity);
            await context.SaveChangesAsync();
            return contentEntity;
        });

    public static Task<List<ContentEntity>> ReadContentEntitiesAsync() =>
        WithWriterAsync(context =>
            context.ContentEntities.OrderBy(entity => entity.Id).ToListAsync()
        );

    public static Task<List<Tombstone>> ReadTombstonesAsync() =>
        WithWriterAsync(context =>
            context.Tombstones.OrderBy(tombstone => tombstone.Id).ToListAsync()
        );

    public static Task<List<EventLogEntry>> ReadEventLogAsync() =>
        WithWriterAsync(context => context.EventLog.OrderBy(entry => entry.Id).ToListAsync());
}
