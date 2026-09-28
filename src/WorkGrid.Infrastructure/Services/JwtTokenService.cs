using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;

namespace WorkGrid.Infrastructure.Services;

public sealed class JwtTokenService : ITokenService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public JwtTokenService(IConfiguration? configuration = null)
    {
        _secret = configuration?["Jwt:Secret"]
            ?? "WorkGrid-Dev-Secret-Key-Must-Be-At-Least-32-Characters-Long!";
        _issuer = configuration?["Jwt:Issuer"] ?? "WorkGrid";
        _audience = configuration?["Jwt:Audience"] ?? "WorkGridClients";
        _expirationMinutes = int.TryParse(configuration?["Jwt:ExpirationMinutes"], out var exp) ? exp : 60;
    }

    public string GenerateToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
