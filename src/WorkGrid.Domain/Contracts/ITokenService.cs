using WorkGrid.Domain.Entities;

namespace WorkGrid.Domain.Contracts;

/// <summary>
/// Generates authentication tokens for verified users.
/// Implementation details (JWT, opaque, etc.) are hidden from consumers.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates an authentication token for the given user.
    /// The caller is responsible for verifying credentials before calling this method.
    /// </summary>
    string GenerateToken(User user);
}
