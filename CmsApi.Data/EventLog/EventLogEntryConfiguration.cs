using CmsApi.Data.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CmsApi.Data.EventLog;

internal sealed class EventLogEntryConfiguration : IEntityTypeConfiguration<EventLogEntry>
{
    private const string TableName = "event_log";
    private const string ContentEntityIdColumn = "entity_id";

    public void Configure(EntityTypeBuilder<EventLogEntry> builder)
    {
        builder.ToTable(TableName);
        builder.Property(entry => entry.ContentEntityId).HasColumnName(ContentEntityIdColumn);
        builder.Property(entry => entry.EventType).HasConversion<string>();
        builder.Property(entry => entry.Outcome).HasConversion<string>();
        builder
            .HasOne<InboxBatch>()
            .WithMany()
            .HasForeignKey(entry => entry.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(entry => new { entry.ContentEntityId, entry.Id });
    }
}
