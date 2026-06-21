using LiveSync.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveSync.Infrastructure.Persistence.Configurations;

public sealed class ChangeQueueEntryConfiguration : IEntityTypeConfiguration<ChangeQueueEntry>
{
    public void Configure(EntityTypeBuilder<ChangeQueueEntry> builder)
    {
        builder.ToTable("ChangeQueue");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Version).HasMaxLength(20).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(2000);
    }
}
