using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace WorkGrid.Infrastructure.Remote;

public sealed class RemoteClient : IRemoteClient, IDisposable
{
    private readonly HttpMessageHandler? _customHandler;
    private HttpClient? _httpClient;
    private ConnectionState _state = ConnectionState.Unknown;
    private string? _authToken;
    private readonly object _lock = new();
    private bool _disposed;

    public ConnectionState State
    {
        get
        {
            lock (_lock) return _state;
        }
        private set
        {
            lock (_lock)
            {
                if (_state == value) return;
                _state = value;
            }
            StateChanged?.Invoke(value);
        }
    }

    public RemoteEndpoint Endpoint { get; private set; }

    public string? AuthToken
    {
        get
        {
            lock (_lock) return _authToken;
        }
    }

    public event Action<ConnectionState>? StateChanged;

    public RemoteClient(RemoteEndpoint endpoint, HttpMessageHandler? customHandler = null)
    {
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _customHandler = customHandler;
        RecreateHttpClient();
    }

    public void UpdateEndpoint(RemoteEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        lock (_lock)
        {
            Endpoint = endpoint;
            RecreateHttpClient();
            State = ConnectionState.Unknown;
        }
    }

    public void SetAuthToken(string? token)
    {
        lock (_lock)
        {
            _authToken = string.IsNullOrWhiteSpace(token) ? null : token.Trim();
            ApplyAuthHeader();
        }
    }

    public void ClearAuthToken()
    {
        SetAuthToken(null);
    }

    private void ApplyAuthHeader()
    {
        if (_httpClient is null) return;

        if (string.IsNullOrEmpty(_authToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _authToken);
        }
    }

    private void RecreateHttpClient()
    {
        _httpClient?.Dispose();

        var handler = _customHandler ?? new HttpClientHandler();
        _httpClient = new HttpClient(handler, disposeHandler: _customHandler == null)
        {
            BaseAddress = Endpoint.BaseUri,
            Timeout = Endpoint.Timeout
        };
        ApplyAuthHeader();
    }

    public async Task<RemoteResult> ExecuteAsync(
        Func<HttpClient, Task<HttpResponseMessage>> request,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(RemoteClient));
        ArgumentNullException.ThrowIfNull(request);

        const int maxRetries = 2; // Total of 3 attempts: initial execution + up to 2 retries
        var retryDelay = TimeSpan.FromMilliseconds(200);

        State = ConnectionState.Connecting;

        for (int attempt = 1; attempt <= maxRetries + 1; attempt++)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (attempt > 1)
                {
                    State = ConnectionState.Recovering;
                    await Task.Delay(retryDelay, cancellationToken);
                }

                var response = await request(_httpClient!);

                // Track state transitions based on outcome
                if (response.IsSuccessStatusCode)
                {
                    State = ConnectionState.Connected;
                    return RemoteResult.Success((int)response.StatusCode);
                }

                // Handle authorization and general status codes
                if (response.StatusCode == HttpStatusCode.Unauthorized ||
                    response.StatusCode == HttpStatusCode.Forbidden)
                {
                    State = ConnectionState.Connected; // Endpoint remains reachable
                    return RemoteResult.Failure(RemoteErrorKind.Unauthorized, "Unauthorized request.", (int)response.StatusCode);
                }

                // Treat server errors (5xx) as transient for retries, others as direct failure
                if ((int)response.StatusCode >= 500 && attempt <= maxRetries)
                {
                    continue; // Loop and retry
                }

                State = ConnectionState.Connected; // HTTP client connected but returned error
                return RemoteResult.Failure(RemoteErrorKind.HttpFailure, $"HTTP response failed with status code {response.StatusCode}", (int)response.StatusCode);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                State = ConnectionState.Disconnected;
                return RemoteResult.Failure(RemoteErrorKind.Cancelled, "Operation cancelled by token.", null);
            }
            catch (TimeoutException)
            {
                if (attempt > maxRetries)
                {
                    State = ConnectionState.Disconnected;
                    return RemoteResult.Failure(RemoteErrorKind.Timeout, "The connection request timed out.", null);
                }
            }
            catch (TaskCanceledException)
            {
                if (attempt > maxRetries)
                {
                    State = ConnectionState.Disconnected;
                    return RemoteResult.Failure(RemoteErrorKind.Timeout, "The connection timed out (TaskCanceledException).", null);
                }
            }
            catch (HttpRequestException ex)
            {
                if (attempt > maxRetries)
                {
                    State = ConnectionState.Disconnected;
                    return RemoteResult.Failure(RemoteErrorKind.Unreachable, $"Remote host unreachable: {ex.Message}", null);
                }
            }
            catch (Exception ex)
            {
                State = ConnectionState.Disconnected;
                return RemoteResult.Failure(RemoteErrorKind.Unknown, $"Unexpected system error: {ex.Message}", null);
            }
        }

        State = ConnectionState.Disconnected;
        return RemoteResult.Failure(RemoteErrorKind.Unreachable, "Exhausted all transient retry attempts.", null);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _httpClient?.Dispose();
        _disposed = true;
    }
}
