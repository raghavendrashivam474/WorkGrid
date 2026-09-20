using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkGrid.Domain.Contracts;
using WorkGrid.Infrastructure.Persistence;
using WorkGrid.Infrastructure.Repositories;
using WorkGrid.Infrastructure.Services;

namespace WorkGrid.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string databasePath)
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

        return services;
    }
}
