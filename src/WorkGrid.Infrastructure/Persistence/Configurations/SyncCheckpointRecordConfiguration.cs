using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WorkGrid.Infrastructure.Persistence.Configurations;

public sealed class SyncCheckpointRecordConfiguration : IEntityTypeConfiguration<SyncCheckpointRecord>
{
    public void Configure(EntityTypeBuilder<SyncCheckpointRecord> builder)
    {
        builder.ToTable("SyncCheckpoints");
        builder.HasKey(c => c.RemoteReplicaId);
    }
}
