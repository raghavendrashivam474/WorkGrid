using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Exceptions;

namespace WorkGrid.Domain.Tests;

public class EmployeeTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesEmployee()
    {
        var id = Guid.NewGuid();
        var emp = new Employee(id, "EMP-001", "Alice Smith", "alice@workgrid.io", "Engineering");

        Assert.Equal(id, emp.Id);
        Assert.Equal("EMP-001", emp.EmployeeCode);
        Assert.Equal("Alice Smith", emp.Name);
        Assert.Equal("alice@workgrid.io", emp.Email);
        Assert.Equal("Engineering", emp.Department);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidEmployeeCode_ThrowsDomainValidationException(string code)
    {
        Assert.Throws<DomainValidationException>(() =>
            new Employee(Guid.NewGuid(), code, "Alice", "alice@workgrid.io"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidName_ThrowsDomainValidationException(string name)
    {
        Assert.Throws<DomainValidationException>(() =>
            new Employee(Guid.NewGuid(), "EMP-001", name, "alice@workgrid.io"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-email")]
    public void Constructor_WithInvalidEmail_ThrowsDomainValidationException(string email)
    {
        Assert.Throws<DomainValidationException>(() =>
            new Employee(Guid.NewGuid(), "EMP-001", "Alice", email));
    }

    [Fact]
    public void Constructor_WithEmptyId_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            new Employee(Guid.Empty, "EMP-001", "Alice", "alice@workgrid.io"));
    }

    [Fact]
    public void UpdateDetails_WithValidData_UpdatesEmployee()
    {
        var emp = new Employee(Guid.NewGuid(), "EMP-001", "Alice Smith", "alice@workgrid.io");

        emp.UpdateDetails("Alice Johnson", "alice.j@workgrid.io", "Operations");

        Assert.Equal("Alice Johnson", emp.Name);
        Assert.Equal("alice.j@workgrid.io", emp.Email);
        Assert.Equal("Operations", emp.Department);
    }

    [Fact]
    public void UpdateDetails_WithInvalidName_ThrowsDomainValidationException()
    {
        var emp = new Employee(Guid.NewGuid(), "EMP-001", "Alice Smith", "alice@workgrid.io");

        Assert.Throws<DomainValidationException>(() =>
            emp.UpdateDetails(" ", "alice@workgrid.io"));
    }
}
