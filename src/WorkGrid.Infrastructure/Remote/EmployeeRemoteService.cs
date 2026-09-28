using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WorkGrid.Domain.Entities;

namespace WorkGrid.Infrastructure.Remote;

public sealed class EmployeeRemoteService : IEmployeeRemoteService
{
    private readonly IRemoteClient _remoteClient;

    public EmployeeRemoteService(IRemoteClient remoteClient)
    {
        _remoteClient = remoteClient ?? throw new ArgumentNullException(nameof(remoteClient));
    }

    public async Task<RemoteResult<IReadOnlyList<Employee>>> GetEmployeesAsync(CancellationToken cancellationToken = default)
    {
        List<RemoteEmployeeDto>? dtos = null;

        var result = await _remoteClient.ExecuteAsync(async (httpClient) =>
        {
            var response = await httpClient.GetAsync("/api/employees", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                dtos = await response.Content.ReadFromJsonAsync<List<RemoteEmployeeDto>>(cancellationToken: cancellationToken);
            }
            return response;
        }, cancellationToken);

        if (!result.IsSuccess)
        {
            return RemoteResult<IReadOnlyList<Employee>>.Failure(
                result.ErrorKind ?? RemoteErrorKind.Unknown,
                result.ErrorMessage,
                result.StatusCode);
        }

        var employees = (dtos ?? Enumerable.Empty<RemoteEmployeeDto>())
            .Select(d => new Employee(d.Id, d.EmployeeCode, d.Name, d.Email, d.Department))
            .ToList();

        return RemoteResult<IReadOnlyList<Employee>>.Success(employees, result.StatusCode);
    }
}
