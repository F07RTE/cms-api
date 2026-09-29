using CmsApi.Data.ContentEntities;
using CmsApi.Data.EventLog;
using CmsApi.Data.Inbox;
using CmsApi.Data.Tombstones;
using CmsApi.Data.Users;
using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data;

public abstract class CmsDbContext : DbContext
{
    protected CmsDbContext(DbContextOptions options)
        : base(options) { }

    public DbSet<InboxBatch> InboxBatches => Set<InboxBatch>();

    public DbSet<ContentEntity> ContentEntities => Set<ContentEntity>();

    public DbSet<EventLogEntry> EventLog => Set<EventLogEntry>();

    public DbSet<Tombstone> Tombstones => Set<Tombstone>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CmsDbContext).Assembly);
}
