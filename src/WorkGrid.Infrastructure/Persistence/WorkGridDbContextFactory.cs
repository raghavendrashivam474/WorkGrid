using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WorkGrid.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core tooling (migrations, scaffolding).
/// Not used at runtime — MAUI's DI provides the real connection string.
/// </summary>
public sealed class WorkGridDbContextFactory : IDesignTimeDbContextFactory<WorkGridDbContext>
{
    public WorkGridDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WorkGridDbContext>();

        // Use a fixed local path for design-time operations.
        // This database is never used at runtime.
        var designTimeDbPath = Path.Combine(
            Path.GetTempPath(), "workgrid-design.db3");

        optionsBuilder.UseSqlite($"Data Source={designTimeDbPath}");

        return new WorkGridDbContext(optionsBuilder.Options);
    }
}
