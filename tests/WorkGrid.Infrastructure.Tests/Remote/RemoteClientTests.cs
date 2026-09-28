using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WorkGrid.Infrastructure.Remote;
using Xunit;

namespace WorkGrid.Infrastructure.Tests.Remote;

public class RemoteClientTests
{
    private readonly RemoteEndpoint _defaultEndpoint = RemoteEndpoint.FromUrl("https://api.workgrid.test");

    [Fact]
    public void InitialState_IsUnknown()
    {
        using var client = new RemoteClient(_defaultEndpoint);
        Assert.Equal(ConnectionState.Unknown, client.State);
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccessfulRequest_ReturnsSuccessAndConnectedState()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK));

        using var client = new RemoteClient(_defaultEndpoint, handler);
        var stateChanges = new List<ConnectionState>();
        client.StateChanged += stateChanges.Add;

        var result = await client.ExecuteAsync(http => http.GetAsync("/health"));

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(ConnectionState.Connected, client.State);
        Assert.Contains(ConnectionState.Connecting, stateChanges);
        Assert.Contains(ConnectionState.Connected, stateChanges);
    }

    [Fact]
    public async Task ExecuteAsync_On401Unauthorized_ReturnsUnauthorizedFailureAndConnectedState()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        using var client = new RemoteClient(_defaultEndpoint, handler);

        var result = await client.ExecuteAsync(http => http.GetAsync("/secure-data"));

        Assert.False(result.IsSuccess);
        Assert.Equal(RemoteErrorKind.Unauthorized, result.ErrorKind);
        Assert.Equal(401, result.StatusCode);
        Assert.Equal(ConnectionState.Connected, client.State); // Remote host was reached
    }

    [Fact]
    public async Task ExecuteAsync_On404NotFound_ReturnsHttpFailure()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.NotFound));

        using var client = new RemoteClient(_defaultEndpoint, handler);

        var result = await client.ExecuteAsync(http => http.GetAsync("/missing"));

        Assert.False(result.IsSuccess);
        Assert.Equal(RemoteErrorKind.HttpFailure, result.ErrorKind);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(ConnectionState.Connected, client.State);
    }

    [Fact]
    public async Task ExecuteAsync_OnTransientErrorThenSuccess_RecoversAndSucceeds()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError)); // attempt 1 fails
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK));                  // attempt 2 succeeds

        using var client = new RemoteClient(_defaultEndpoint, handler);
        var stateChanges = new List<ConnectionState>();
        client.StateChanged += stateChanges.Add;

        var result = await client.ExecuteAsync(http => http.GetAsync("/data"));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, handler.RequestCount);
        Assert.Equal(ConnectionState.Connected, client.State);
        Assert.Contains(ConnectionState.Recovering, stateChanges);
    }

    [Fact]
    public async Task ExecuteAsync_OnRepeatedTransientErrors_ExhaustsRetriesAndReportsDisconnected()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        using var client = new RemoteClient(_defaultEndpoint, handler);

        var result = await client.ExecuteAsync(http => http.GetAsync("/data"));

        Assert.False(result.IsSuccess);
        Assert.Equal(RemoteErrorKind.HttpFailure, result.ErrorKind);
        Assert.Equal(3, handler.RequestCount); // Initial + 2 retries
        Assert.Equal(ConnectionState.Connected, client.State);
    }

    [Fact]
    public async Task ExecuteAsync_OnHttpRequestExceptionExhaustion_ReportsUnreachableAndDisconnected()
    {
        using var client = new RemoteClient(_defaultEndpoint);

        var result = await client.ExecuteAsync(_ => throw new HttpRequestException("Host not reachable"));

        Assert.False(result.IsSuccess);
        Assert.Equal(RemoteErrorKind.Unreachable, result.ErrorKind);
        Assert.Equal(ConnectionState.Disconnected, client.State);
    }

    [Fact]
    public async Task ExecuteAsync_OnTimeout_ReportsTimeoutAndDisconnected()
    {
        using var client = new RemoteClient(_defaultEndpoint);

        var result = await client.ExecuteAsync(_ => throw new TaskCanceledException());

        Assert.False(result.IsSuccess);
        Assert.Equal(RemoteErrorKind.Timeout, result.ErrorKind);
        Assert.Equal(ConnectionState.Disconnected, client.State);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_ReturnsCancelledState()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        using var client = new RemoteClient(_defaultEndpoint);

        var result = await client.ExecuteAsync(http => http.GetAsync("/data"), cts.Token);

        Assert.False(result.IsSuccess);
        Assert.Equal(RemoteErrorKind.Cancelled, result.ErrorKind);
        Assert.Equal(ConnectionState.Disconnected, client.State);
    }

    [Fact]
    public async Task UpdateEndpoint_ResetsStateToUnknown()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK));

        using var client = new RemoteClient(_defaultEndpoint, handler);
        var _ = await client.ExecuteAsync(http => http.GetAsync("/health"));
        Assert.Equal(ConnectionState.Connected, client.State);

        var newEndpoint = RemoteEndpoint.FromUrl("https://api2.workgrid.test");
        client.UpdateEndpoint(newEndpoint);

        Assert.Equal(ConnectionState.Unknown, client.State);
        Assert.Equal(newEndpoint, client.Endpoint);
    }
}
