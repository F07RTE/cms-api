using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CmsApi.Data.Tombstones;

internal sealed class TombstoneConfiguration : IEntityTypeConfiguration<Tombstone>
{
    private const string TableName = "tombstones";

    public void Configure(EntityTypeBuilder<Tombstone> builder) => builder.ToTable(TableName);
}
