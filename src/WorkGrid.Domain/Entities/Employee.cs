using WorkGrid.Domain.Exceptions;

namespace WorkGrid.Domain.Entities;

public sealed class Employee
{
    public Guid Id { get; private set; }
    public string EmployeeCode { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string? Department { get; private set; }

    public Employee(Guid id, string employeeCode, string name, string email, string? department = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Employee ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            throw new DomainValidationException("Employee code cannot be null or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Employee name cannot be null or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            throw new DomainValidationException("Employee email must be a valid non-empty email address.");
        }

        Id = id;
        EmployeeCode = employeeCode.Trim();
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Department = department?.Trim();
    }

    public void UpdateDetails(string name, string email, string? department = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Employee name cannot be null or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            throw new DomainValidationException("Employee email must be a valid non-empty email address.");
        }

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Department = department?.Trim();
    }
}
