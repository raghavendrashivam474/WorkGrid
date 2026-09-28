namespace WorkGrid.Infrastructure.Remote;

/// <summary>
/// Categorises remote failures at the transport level.
/// Business/validation errors are NOT represented here.
/// </summary>
public enum RemoteErrorKind
{
    Timeout,
    Unreachable,
    HttpFailure,
    Unauthorized,
    Cancelled,
    Unknown
}
