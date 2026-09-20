using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkGrid.Domain.Entities;

namespace WorkGrid.Infrastructure.Persistence.Configurations;

public sealed class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EmployeeId)
            .IsRequired();

        builder.Property(a => a.AssetId)
            .IsRequired();

        // Map DateTimeOffset to ISO-8601 strings for robust SQLite sorting and querying
        builder.Property(a => a.AssignedAt)
            .HasConversion(
                v => v.ToString("o", CultureInfo.InvariantCulture),
                v => DateTimeOffset.Parse(v, CultureInfo.InvariantCulture))
            .IsRequired();

        builder.Property(a => a.ReturnedAt)
            .HasConversion(
                v => v.HasValue ? v.Value.ToString("o", CultureInfo.InvariantCulture) : null,
                v => v != null ? DateTimeOffset.Parse(v, CultureInfo.InvariantCulture) : (DateTimeOffset?)null)
            .IsRequired(false);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<int>();

        // Indexes for lookups
        builder.HasIndex(a => a.EmployeeId);
        builder.HasIndex(a => a.AssetId);
    }
}
