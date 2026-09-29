using CmsApi.Data.Inbox;
using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data;

public abstract class CmsDbContext : DbContext
{
    protected CmsDbContext(DbContextOptions options)
        : base(options) { }

    public DbSet<InboxBatch> InboxBatches => Set<InboxBatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CmsDbContext).Assembly);
}
