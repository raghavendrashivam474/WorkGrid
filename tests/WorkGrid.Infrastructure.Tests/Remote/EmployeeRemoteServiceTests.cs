using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WorkGrid.Infrastructure.Remote;
using Xunit;

namespace WorkGrid.Infrastructure.Tests.Remote;

public sealed class EmployeeRemoteServiceTests
{
    [Fact]
    public async Task GetEmployeesAsync_WhenApiReturns200_MapsToDomainEmployees()
    {
        // Arrange
        var empId = Guid.NewGuid();
        var dtos = new[]
        {
            new RemoteEmployeeDto(empId, "EMP-001", "Alice Smith", "alice@workgrid.com", "Engineering")
        };
        var json = JsonSerializer.Serialize(dtos);

        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });

        var endpoint = new RemoteEndpoint(new Uri("https://api.workgrid.local"));
        using var client = new RemoteClient(endpoint, handler);
        var service = new EmployeeRemoteService(client);

        // Act
        var result = await service.GetEmployeesAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        var emp = Assert.Single(result.Data);
        Assert.Equal(empId, emp.Id);
        Assert.Equal("EMP-001", emp.EmployeeCode);
        Assert.Equal("Alice Smith", emp.Name);
        Assert.Equal("alice@workgrid.com", emp.Email);
        Assert.Equal("Engineering", emp.Department);
    }

    [Fact]
    public async Task GetEmployeesAsync_WhenApiReturns401_ReturnsUnauthorizedFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var endpoint = new RemoteEndpoint(new Uri("https://api.workgrid.local"));
        using var client = new RemoteClient(endpoint, handler);
        var service = new EmployeeRemoteService(client);

        // Act
        var result = await service.GetEmployeesAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(RemoteErrorKind.Unauthorized, result.ErrorKind);
        Assert.Equal(401, result.StatusCode);
        Assert.Null(result.Data);
    }
}
