using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WorkGrid.Infrastructure.Remote;

/// <summary>
/// Clean boundary for executing remote HTTP operations with transient-failure
/// recovery, timeout handling, connection state reporting, and credential management.
/// </summary>
public interface IRemoteClient
{
    ConnectionState State { get; }
    RemoteEndpoint Endpoint { get; }
    string? AuthToken { get; }

    event Action<ConnectionState>? StateChanged;

    /// <summary>
    /// Executes an HTTP request against the remote API.
    /// Manages timeouts, retries, authentication headers, and state transitions automatically.
    /// </summary>
    Task<RemoteResult> ExecuteAsync(
        Func<HttpClient, Task<HttpResponseMessage>> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the target API endpoint and resets state back to Unknown.
    /// </summary>
    void UpdateEndpoint(RemoteEndpoint endpoint);

    /// <summary>
    /// Sets or clears the authentication token attached to outgoing requests.
    /// </summary>
    void SetAuthToken(string? token);

    /// <summary>
    /// Clears the authentication token.
    /// </summary>
    void ClearAuthToken();
}
