using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WorkGrid.Infrastructure.Persistence.Configurations;

public sealed class SyncChangeRecordConfiguration : IEntityTypeConfiguration<SyncChangeRecord>
{
    public void Configure(EntityTypeBuilder<SyncChangeRecord> builder)
    {
        builder.ToTable("SyncChanges");
        builder.HasKey(c => c.ChangeId);

        builder.Property(c => c.ObjectType).IsRequired();
        builder.Property(c => c.ObjectId).IsRequired();
        builder.Property(c => c.Operation).IsRequired();
        builder.Property(c => c.OriginatingDeviceId).IsRequired();
        builder.Property(c => c.SequenceNumber).IsRequired();
        builder.Property(c => c.PayloadHash)
            .IsRequired()
            .HasMaxLength(64);
        builder.Property(c => c.Payload).IsRequired(false);

        // Primary query pattern: get max sequence for a device
        builder.HasIndex(c => new { c.OriginatingDeviceId, c.SequenceNumber });

        // Lookup pattern: find changes for a specific object
        builder.HasIndex(c => new { c.ObjectType, c.ObjectId });

        // Manifest/reconciliation pattern: changes per device per type
        builder.HasIndex(c => new { c.OriginatingDeviceId, c.ObjectType, c.ObjectId });
    }
}
