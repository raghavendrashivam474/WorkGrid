using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using WorkGrid.Infrastructure.DependencyInjection;
using WorkGrid.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add API controllers
builder.Services.AddControllers().AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new WorkGrid.Infrastructure.Remote.Sync.SyncObjectKeyJsonConverter()); });
builder.Services.AddSingleton<WorkGrid.Api.Sync.RelayCoordinator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Server Persistence
var connectionString = builder.Configuration.GetConnectionString("WorkGridServerDb");
var serverDbPath = string.IsNullOrWhiteSpace(connectionString)
    ? Path.Combine(AppContext.BaseDirectory, "workgrid_server.db")
    : (Path.IsPathRooted(connectionString) ? connectionString : Path.Combine(AppContext.BaseDirectory, connectionString));

builder.Services.AddInfrastructure(serverDbPath);

// Configure JWT Authentication (S4.3)
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "WorkGrid";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "WorkGridClients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Ensure server database schema is created on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WorkGridDbContext>();
    dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }


