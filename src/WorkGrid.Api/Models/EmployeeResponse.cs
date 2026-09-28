using System;
using WorkGrid.Domain.Entities;

namespace WorkGrid.Api.Models;

/// <summary>
/// Public API DTO for employee representation.
/// Isolates the external HTTP contract from internal domain entity changes.
/// </summary>
public sealed record EmployeeResponse(
    Guid Id,
    string EmployeeCode,
    string Name,
    string Email,
    string? Department);

/// <summary>
/// Explicit mapping extensions between Domain entities and API DTOs.
/// </summary>
public static class EmployeeMappingExtensions
{
    public static EmployeeResponse ToResponse(this Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        return new EmployeeResponse(
            employee.Id,
            employee.EmployeeCode,
            employee.Name,
            employee.Email,
            employee.Department);
    }
}
