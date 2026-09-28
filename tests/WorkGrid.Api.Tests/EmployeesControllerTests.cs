using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkGrid.Api.Controllers;
using WorkGrid.Api.Models;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using Xunit;

namespace WorkGrid.Api.Tests;

public sealed class EmployeesControllerTests
{
    private sealed class MockLogger : ILogger<EmployeesController>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    private sealed class MockEmployeeRepository : IEmployeeRepository
    {
        public Func<CancellationToken, Task<IReadOnlyList<Employee>>>? GetAllAsyncFunc { get; set; }

        public Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return GetAllAsyncFunc != null 
                ? GetAllAsyncFunc(cancellationToken) 
                : Task.FromResult<IReadOnlyList<Employee>>(Array.Empty<Employee>());
        }

        public Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExistsByCodeAsync(string employeeCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAsync(Employee employee, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAsync(Employee employee, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithMappedEmployees()
    {
        // Arrange
        var mockRepo = new MockEmployeeRepository();
        var employeesList = new List<Employee>
        {
            new(Guid.NewGuid(), "EMP-1", "Alice", "alice@workgrid.com", "Engineering"),
            new(Guid.NewGuid(), "EMP-2", "Bob", "bob@workgrid.com", "Product")
        };
        mockRepo.GetAllAsyncFunc = _ => Task.FromResult<IReadOnlyList<Employee>>(employeesList);

        var controller = new EmployeesController(mockRepo, new MockLogger());

        // Act
        var result = await controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEmployees = Assert.IsAssignableFrom<IEnumerable<EmployeeResponse>>(okResult.Value);
        Assert.Equal(2, returnedEmployees.Count());
    }

    [Fact]
    public async Task GetAll_OnException_Returns500WithSafePayload()
    {
        // Arrange
        var mockRepo = new MockEmployeeRepository();
        mockRepo.GetAllAsyncFunc = _ => throw new InvalidOperationException("Db Connection blown up!");

        var controller = new EmployeesController(mockRepo, new MockLogger());

        // Act
        var result = await controller.GetAll(CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objectResult.StatusCode);
        
        // Assert stack traces or internal secrets are not leaked
        var payload = objectResult.Value;
        Assert.NotNull(payload);
        var errorProp = payload.GetType().GetProperty("error");
        Assert.NotNull(errorProp);
        var errorMessage = errorProp.GetValue(payload) as string;
        Assert.Equal("An unexpected error occurred while processing your request.", errorMessage);
    }
}
