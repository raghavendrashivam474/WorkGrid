using System;
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
public sealed class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Authenticates a user and returns a JWT bearer token.
    /// Deliberately bypasses ISessionService to remain stateless on the server.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized(new { error = "Username and password are required." });
        }

        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

        if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { error = "Invalid username or password." });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { error = "This user account is inactive." });
        }

        var token = _tokenService.GenerateToken(user);

        _logger.LogInformation("User '{Username}' authenticated successfully via API.", user.Username);

        return Ok(new LoginResponse(token, user.Username, user.Role.ToString()));
    }
}
