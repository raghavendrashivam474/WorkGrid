using Microsoft.EntityFrameworkCore;
using WorkGrid.Domain.Entities;

namespace WorkGrid.Infrastructure.Persistence;

public sealed class WorkGridDbContext : DbContext
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Assignment> Assignments => Set<Assignment>();

    public WorkGridDbContext(DbContextOptions<WorkGridDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkGridDbContext).Assembly);
    }
}
