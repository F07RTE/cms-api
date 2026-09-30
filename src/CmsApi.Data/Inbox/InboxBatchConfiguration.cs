using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CmsApi.Data.Inbox;

internal sealed class InboxBatchConfiguration : IEntityTypeConfiguration<InboxBatch>
{
    private const string TableName = "inbox";

    // text, not jsonb: jsonb rejects \u0000 and collapses duplicate keys, both per-event Failed cases.
    private const string BodyColumnType = "text";

    public void Configure(EntityTypeBuilder<InboxBatch> builder)
    {
        builder.ToTable(TableName);
        builder.Property(batch => batch.Body).HasColumnType(BodyColumnType);
        builder.Property(batch => batch.Status).HasConversion<string>();
    }
}
