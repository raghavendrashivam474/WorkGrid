using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WorkGrid.Infrastructure.DependencyInjection;
using WorkGrid.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add API controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Server Persistence (explicitly distinct from mobile SQLite path)
var connectionString = builder.Configuration.GetConnectionString("WorkGridServerDb");
var serverDbPath = string.IsNullOrWhiteSpace(connectionString)
    ? Path.Combine(AppContext.BaseDirectory, "workgrid_server.db")
    : (Path.IsPathRooted(connectionString) ? connectionString : Path.Combine(AppContext.BaseDirectory, connectionString));

builder.Services.AddInfrastructure(serverDbPath);

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
app.UseAuthorization();
app.MapControllers();

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
