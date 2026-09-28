using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkGrid.Api.Models;
using WorkGrid.Domain.Entities;
using WorkGrid.Infrastructure.Persistence;
using Xunit;

namespace WorkGrid.Api.Tests;

public sealed class EmployeesApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _testDbPath;

    public EmployeesApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _testDbPath = Path.Combine(AppContext.BaseDirectory, $"integration_test_{Guid.NewGuid():N}.db");
        
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DB configuration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<WorkGridDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Inject temporary integration database context
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
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // Ignore test teardown resource locks
            }
        }
    }

    [Fact]
    public async Task Get_EmployeesEndpoint_ReturnsEmptyArrayWhenDbIsEmpty()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Ensure database schema is built fresh
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WorkGridDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }

        // Act
        var response = await client.GetAsync("/api/employees");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var employees = await response.Content.ReadFromJsonAsync<IEnumerable<EmployeeResponse>>();
        Assert.NotNull(employees);
        Assert.Empty(employees);
    }

    [Fact]
    public async Task Get_EmployeesEndpoint_ReturnsSeededEmployees()
    {
        // Arrange
        var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WorkGridDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            var seedEmp = new Employee(Guid.NewGuid(), "EMP-INTEG-1", "Integration Bob", "bob.integ@workgrid.com", "QA");
            await db.Employees.AddAsync(seedEmp);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync("/api/employees");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var employees = await response.Content.ReadFromJsonAsync<List<EmployeeResponse>>();
        Assert.NotNull(employees);
        var singleEmp = Assert.Single(employees);
        Assert.Equal("EMP-INTEG-1", singleEmp.EmployeeCode);
        Assert.Equal("Integration Bob", singleEmp.Name);
    }
}
