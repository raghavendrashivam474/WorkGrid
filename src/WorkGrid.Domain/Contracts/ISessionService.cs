using WorkGrid.Domain.Entities;

namespace WorkGrid.Domain.Contracts;

public interface ISessionService
{
    User? CurrentUser { get; }
    bool IsAuthenticated { get; }
    void StartSession(User user);
    void EndSession();
}
