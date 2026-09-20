using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Exceptions;

namespace WorkGrid.Infrastructure.Services;

public sealed class SessionService : ISessionService
{
    public User? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null && CurrentUser.IsActive;

    public void StartSession(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!user.IsActive)
            throw new DomainValidationException("Cannot start session for an inactive user.");

        CurrentUser = user;
    }

    public void EndSession()
    {
        CurrentUser = null;
    }
}
