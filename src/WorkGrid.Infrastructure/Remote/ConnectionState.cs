namespace WorkGrid.Infrastructure.Remote;

/// <summary>
/// Represents the current state of connectivity to the remote WorkGrid API.
/// </summary>
public enum ConnectionState
{
    Unknown,
    Connecting,
    Connected,
    Disconnected,
    Recovering
}
