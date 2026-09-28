using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkGrid.Api.Models;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Infrastructure.Persistence;
using WorkGrid.Infrastructure.Services;
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

    private async Task<string> CreateUserAndGetTokenAsync(string username, string password, UserRole role)
    {
        var hasher = new PasswordHasher();
        var tokenService = new JwtTokenService();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkGridDbContext>();
        var user = new User(Guid.NewGuid(), username, hasher.HashPassword(password), "Test User", role);
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        return tokenService.GenerateToken(user);
    }

    [Fact]
    public async Task Get_Employees_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/employees");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_EmployeesEndpoint_WithValidBearerToken_ReturnsSeededEmployees()
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

        var token = await CreateUserAndGetTokenAsync("api_tester", "Pass123!", UserRole.Viewer);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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

    [Fact]
    public async Task AuthLogin_WithValidCredentials_ReturnsTokenAndRole()
    {
        // Arrange
        var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WorkGridDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            var hasher = new PasswordHasher();
            var user = new User(Guid.NewGuid(), "manager_bob", hasher.HashPassword("ManagerPass123!"), "Manager Bob", UserRole.Manager);
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();
        }

        // Act
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("manager_bob", "ManagerPass123!"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var payload = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.Token));
        Assert.Equal("manager_bob", payload.Username);
        Assert.Equal("Manager", payload.Role);
    }
}
