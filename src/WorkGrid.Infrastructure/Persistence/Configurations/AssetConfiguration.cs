using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkGrid.Domain.Entities;

namespace WorkGrid.Infrastructure.Persistence.Configurations;

public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AssetTag)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(a => a.AssetTag)
            .IsUnique();

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.AssetType)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(a => a.SerialNumber)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<int>();
    }
}
