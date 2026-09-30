using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CmsApi.Data.ContentEntities;

internal sealed class ContentEntityConfiguration : IEntityTypeConfiguration<ContentEntity>
{
    private const string TableName = "content_entities";
    private const string PayloadColumnType = "jsonb";

    // Named explicitly because the partial index filter below is raw SQL.
    private const string IsPublishedColumn = "is_published";
    private const string IsDisabledByAdminColumn = "is_disabled_by_admin";

    private const string VisibleListIndex = "ix_content_entities_visible_last_event_at_id";
    private const string AdminListIndex = "ix_content_entities_last_event_at_id";

    public void Configure(EntityTypeBuilder<ContentEntity> builder)
    {
        builder.ToTable(TableName);
        builder.Property(entity => entity.Payload).HasColumnType(PayloadColumnType);
        builder.Property(entity => entity.IsPublished).HasColumnName(IsPublishedColumn);
        builder.Property(entity => entity.IsDisabledByAdmin).HasColumnName(IsDisabledByAdminColumn);
        builder
            .HasIndex(entity => new { entity.LastEventAt, entity.Id }, VisibleListIndex)
            .HasDatabaseName(VisibleListIndex)
            .IsDescending(true, false)
            .HasFilter($"{IsPublishedColumn} AND NOT {IsDisabledByAdminColumn}");
        builder
            .HasIndex(entity => new { entity.LastEventAt, entity.Id }, AdminListIndex)
            .HasDatabaseName(AdminListIndex)
            .IsDescending(true, false);
    }
}
