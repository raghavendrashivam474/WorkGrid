using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Infrastructure.Persistence;
using WorkGrid.Infrastructure.Remote;
using WorkGrid.Infrastructure.Services;
using Xunit;

namespace WorkGrid.Api.Tests;

public sealed class EmployeeRemoteVerticalSliceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _testDbPath;

    public EmployeeRemoteVerticalSliceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _testDbPath = Path.Combine(AppContext.BaseDirectory, $"vslice_test_{Guid.NewGuid():N}.db");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<WorkGridDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<WorkGridDbContext>(options =>
                {
                    options.UseSqlite($"Data Source={_testDbPath}");
                });
            });
        });
    }

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
        {
            try { File.Delete(_testDbPath); } catch { }
        }
    }

    [Fact]
    public async Task CompleteVerticalSlice_ClientAuthenticatesAndFetchesDomainEmployeesFromApiDatabase()
    {
        // 1. Arrange Server Database State
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WorkGridDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            var hasher = new PasswordHasher();
            var testUser = new User(Guid.NewGuid(), "vslice_user", hasher.HashPassword("SlicePass123!"), "Slice User", UserRole.Viewer);
            await db.Users.AddAsync(testUser);

            var emp1 = new Employee(Guid.NewGuid(), "EMP-VS-1", "Vertical Slice Alice", "alice.slice@workgrid.com", "Operations");
            var emp2 = new Employee(Guid.NewGuid(), "EMP-VS-2", "Vertical Slice Bob", "bob.slice@workgrid.com", "Logistics");
            await db.Employees.AddRangeAsync(emp1, emp2);
            await db.SaveChangesAsync();
        }

        // 2. Generate Real Client Token using JWT Token Service
        var tokenService = new JwtTokenService();
        var token = tokenService.GenerateToken(new User(Guid.NewGuid(), "vslice_user", "hash", "Slice User", UserRole.Viewer));

        // 3. Configure Client-side RemoteClient hooked to WebApplicationFactory
        var clientHandler = _factory.Server.CreateHandler();
        var endpoint = new RemoteEndpoint(new Uri("http://localhost"));
        using var remoteClient = new RemoteClient(endpoint, clientHandler);

        // Attach Token to Remote Boundary
        remoteClient.SetAuthToken(token);

        // 4. Instantiate High-level Application Service
        var remoteEmployeeService = new EmployeeRemoteService(remoteClient);

        // 5. Act: Execute Remote Fetch
        var result = await remoteEmployeeService.GetEmployeesAsync();

        // 6. Assert Complete Vertical Integrity
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);

        Assert.Contains(result.Data, e => e.EmployeeCode == "EMP-VS-1" && e.Name == "Vertical Slice Alice" && e.Department == "Operations");
        Assert.Contains(result.Data, e => e.EmployeeCode == "EMP-VS-2" && e.Name == "Vertical Slice Bob" && e.Department == "Logistics");
    }
}
