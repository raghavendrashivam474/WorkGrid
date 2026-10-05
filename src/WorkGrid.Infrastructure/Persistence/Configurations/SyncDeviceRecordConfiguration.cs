using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WorkGrid.Infrastructure.Persistence.Configurations;

public sealed class SyncDeviceRecordConfiguration : IEntityTypeConfiguration<SyncDeviceRecord>
{
    public void Configure(EntityTypeBuilder<SyncDeviceRecord> builder)
    {
        builder.ToTable("SyncDevices");
        builder.HasKey(d => d.DeviceId);
    }
}
