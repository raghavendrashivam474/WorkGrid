using System;

namespace WorkGrid.Infrastructure.Remote;

/// <summary>
/// DTO representing employee JSON payload received from WorkGrid.Api.
/// </summary>
public sealed record RemoteEmployeeDto(
    Guid Id,
    string EmployeeCode,
    string Name,
    string Email,
    string? Department);
