using CmsApi.Data.ContentEntities;
using CmsApi.Data.EventLog;
using CmsApi.Data.Inbox;
using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data;

public abstract class CmsDbContext : DbContext
{
    protected CmsDbContext(DbContextOptions options)
        : base(options) { }

    public DbSet<InboxBatch> InboxBatches => Set<InboxBatch>();

    public DbSet<ContentEntity> ContentEntities => Set<ContentEntity>();

    public DbSet<EventLogEntry> EventLog => Set<EventLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CmsDbContext).Assembly);
}
