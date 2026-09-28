using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkGrid.Api.Models;
using WorkGrid.Domain.Contracts;

namespace WorkGrid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class EmployeesController : ControllerBase
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(
        IEmployeeRepository employeeRepository,
        ILogger<EmployeesController> logger)
    {
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all employees from the server repository.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<EmployeeResponse>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var employees = await _employeeRepository.GetAllAsync(cancellationToken);
            var response = employees.Select(e => e.ToResponse());
            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Employee retrieval operation was cancelled.");
            return StatusCode(499); // Client Closed Request
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving employees.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred while processing your request." });
        }
    }
}
