using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkGrid.Domain.Contracts;
using WorkGrid.Infrastructure.Persistence;
using WorkGrid.Infrastructure.Remote;
using WorkGrid.Infrastructure.Repositories;
using WorkGrid.Infrastructure.Services;

namespace WorkGrid.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        string databasePath,
        RemoteEndpoint? remoteEndpoint = null)
    {
        services.AddDbContext<WorkGridDbContext>(options =>
        {
            options.UseSqlite($"Data Source={databasePath}");
        });

        // Repositories
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<IAssignmentRepository, AssignmentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Domain & Application Services
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        // Remote Boundary Foundation (S4.1)
        var endpoint = remoteEndpoint ?? new RemoteEndpoint(new Uri("https://localhost:5001"));
        services.AddSingleton<IRemoteClient>(sp => new RemoteClient(endpoint));

        // Token Service (S4.3)
        services.AddSingleton<ITokenService, JwtTokenService>();

        // Remote Data Services (S4.4)
        services.AddScoped<IEmployeeRemoteService, EmployeeRemoteService>();
        services.AddScoped<WorkGrid.Domain.Sync.ISyncEngine, WorkGrid.Infrastructure.Sync.SyncEngine>();
        services.AddScoped<WorkGrid.Infrastructure.Remote.Sync.ISyncTransport, WorkGrid.Infrastructure.Remote.Sync.HttpSyncRelayTransport>();

        return services;
    }
}




