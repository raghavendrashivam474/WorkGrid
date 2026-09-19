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

        builder.Property(a => a.AssignedAt)
            .IsRequired();

        builder.Property(a => a.ReturnedAt)
            .IsRequired(false);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<int>();

        // Indexes for lookups
        builder.HasIndex(a => a.EmployeeId);
        builder.HasIndex(a => a.AssetId);
    }
}
