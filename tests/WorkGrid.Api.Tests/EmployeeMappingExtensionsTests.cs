using System;
using WorkGrid.Api.Models;
using WorkGrid.Domain.Entities;
using Xunit;

namespace WorkGrid.Api.Tests;

public sealed class EmployeeMappingExtensionsTests
{
    [Fact]
    public void ToResponse_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var employee = new Employee(
            Guid.NewGuid(),
            "EMP-999",
            "SpongeBob SquarePants",
            "spongebob@bikinibottom.com",
            "Kitchen"
        );

        // Act
        var response = employee.ToResponse();

        // Assert
        Assert.Equal(employee.Id, response.Id);
        Assert.Equal(employee.EmployeeCode, response.EmployeeCode);
        Assert.Equal(employee.Name, response.Name);
        Assert.Equal(employee.Email, response.Email);
        Assert.Equal(employee.Department, response.Department);
    }

    [Fact]
    public void ToResponse_WithNullEmployee_ShouldThrowArgumentNullException()
    {
        // Arrange
        Employee nullEmployee = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullEmployee.ToResponse());
    }
}
