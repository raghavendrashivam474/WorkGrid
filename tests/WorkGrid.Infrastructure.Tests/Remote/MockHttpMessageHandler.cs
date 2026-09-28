using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WorkGrid.Infrastructure.Tests.Remote;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();
    public int RequestCount { get; private set; }

    public void EnqueueResponse(HttpResponseMessage response)
    {
        _responses.Enqueue(_ => response);
    }

    public void EnqueueCallback(Func<HttpRequestMessage, HttpResponseMessage> callback)
    {
        _responses.Enqueue(callback);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestCount++;
        cancellationToken.ThrowIfCancellationRequested();

        if (_responses.Count > 0)
        {
            var handler = _responses.Dequeue();
            return Task.FromResult(handler(request));
        }

        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}
