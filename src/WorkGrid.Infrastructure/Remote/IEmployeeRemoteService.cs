using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkGrid.Domain.Entities;

namespace WorkGrid.Infrastructure.Remote;

/// <summary>
/// High-level client abstraction for remote employee operations.
/// Encapsulates HTTP serialization, endpoint paths, and DTO-to-Domain mapping.
/// </summary>
public interface IEmployeeRemoteService
{
    Task<RemoteResult<IReadOnlyList<Employee>>> GetEmployeesAsync(CancellationToken cancellationToken = default);
}
