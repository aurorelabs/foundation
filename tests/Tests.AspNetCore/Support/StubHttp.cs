using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.AspNetCore.Support;

/// <summary>A handler that answers every request itself and keeps what it was sent, for the tests of outbound HTTP components.</summary>
internal sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond, TimeSpan? delay = null) : HttpMessageHandler
{
    private readonly Lock _gate = new();

    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string> Bodies { get; } = [];

    public int Calls
    {
        get
        {
            lock (_gate)
            {
                return Requests.Count;
            }
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);

        lock (_gate)
        {
            Requests.Add(request);
            Bodies.Add(body);
        }

        if (delay is { } wait)
            await Task.Delay(wait, cancellationToken);

        return respond(request);
    }
}

/// <summary>An IHttpClientFactory that always hands out a client on the given handler.</summary>
internal sealed class StubClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        return new HttpClient(handler, disposeHandler: false);
    }
}
