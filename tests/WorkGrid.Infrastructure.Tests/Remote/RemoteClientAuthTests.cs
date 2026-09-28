using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using WorkGrid.Infrastructure.Remote;
using Xunit;

namespace WorkGrid.Infrastructure.Tests.Remote;

public sealed class RemoteClientAuthTests
{
    [Fact]
    public void SetAuthToken_UpdatesAuthTokenProperty()
    {
        var endpoint = new RemoteEndpoint(new Uri("https://api.workgrid.local"));
        using var client = new RemoteClient(endpoint);

        client.SetAuthToken("sample-token-123");
        Assert.Equal("sample-token-123", client.AuthToken);

        client.ClearAuthToken();
        Assert.Null(client.AuthToken);
    }

    [Fact]
    public async Task ExecuteAsync_WithAuthToken_IncludesAuthorizationHeader()
    {
        var endpoint = new RemoteEndpoint(new Uri("https://api.workgrid.local"));
        string? capturedHeader = null;

        var handler = new MockHttpMessageHandler();
        handler.EnqueueCallback(req =>
        {
            capturedHeader = req.Headers.Authorization?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var client = new RemoteClient(endpoint, handler);
        client.SetAuthToken("jwt-token-xyz");

        var result = await client.ExecuteAsync(http => http.GetAsync("/api/protected"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Bearer jwt-token-xyz", capturedHeader);
    }

    [Fact]
    public async Task ExecuteAsync_When401Returned_MapsToUnauthorizedError()
    {
        var endpoint = new RemoteEndpoint(new Uri("https://api.workgrid.local"));

        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        using var client = new RemoteClient(endpoint, handler);

        var result = await client.ExecuteAsync(http => http.GetAsync("/api/protected"));

        Assert.False(result.IsSuccess);
        Assert.Equal(RemoteErrorKind.Unauthorized, result.ErrorKind);
        Assert.Equal(401, result.StatusCode);
    }
}
