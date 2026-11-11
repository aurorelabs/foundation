using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.HttpDependencies.Handlers;
using Aurore.Foundation.AspNetCore.HttpDependencies.Services;
using Aurore.Foundation.Core.HttpDependencies.Models;
using Aurore.Foundation.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Tests.AspNetCore.Support;

namespace Tests.AspNetCore.HttpDependencies.Handlers;

[Trait("Feature", "Outbound HTTP handlers")]
public sealed class OutboundHandlersTests
{
    [Fact(DisplayName = "The upstream API key is sent in its header, replacing one the caller set")]
    public async Task ApiKeyIsSentInItsHeader()
    {
        // Arrange
        var inner = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var invoker = new HttpMessageInvoker(new ApiKeyHandler(new UpstreamApiKeyOptions { HeaderName = "X-Inventory-Key", Value = "upstream-secret" }) { InnerHandler = inner });
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://upstream.example/items");
        request.Headers.TryAddWithoutValidation("X-Inventory-Key", "from-the-caller");

        // Act
        await invoker.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["upstream-secret"], inner.Requests[0].Headers.GetValues("X-Inventory-Key"));
    }

    [Fact(DisplayName = "The correlation id of the current request is passed on to the upstream call")]
    public async Task CorrelationIdIsPassedOn()
    {
        // Arrange
        var inner = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var invoker = new HttpMessageInvoker(new CorrelationIdPropagationHandler(AccessorWithCorrelationId("0f8fad5b-d9cb-469f")) { InnerHandler = inner });
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://upstream.example/items");

        // Act
        await invoker.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["0f8fad5b-d9cb-469f"], inner.Requests[0].Headers.GetValues("Correlation-Id"));
    }

    [Fact(DisplayName = "A correlation id the call already carries is kept")]
    public async Task ExistingCorrelationIdIsKept()
    {
        // Arrange
        var inner = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var invoker = new HttpMessageInvoker(new CorrelationIdPropagationHandler(AccessorWithCorrelationId("from-the-request")) { InnerHandler = inner });
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://upstream.example/items");
        request.Headers.TryAddWithoutValidation("Correlation-Id", "set-by-the-feature");

        // Act
        await invoker.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["set-by-the-feature"], inner.Requests[0].Headers.GetValues("Correlation-Id"));
    }

    [Fact(DisplayName = "Outside a request there is no correlation id to pass on")]
    public async Task NoCorrelationIdOutsideARequest()
    {
        // Arrange
        var inner = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var invoker = new HttpMessageInvoker(new CorrelationIdPropagationHandler(new HttpContextAccessor()) { InnerHandler = inner });
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://upstream.example/items");

        // Act
        await invoker.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(inner.Requests[0].Headers.Contains("Correlation-Id"));
    }

    [Fact(DisplayName = "Client credentials calls carry the cached token as a bearer token")]
    public async Task ClientCredentialsCallsCarryTheBearerToken()
    {
        // Arrange
        var tokenEndpoint = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"access_token":"cached-token","expires_in":300}""", Encoding.UTF8, "application/json"),
        });
        var cache = new ClientCredentialsTokenCache(new StubClientFactory(tokenEndpoint), new FakeTimeProvider());
        var inner = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var options = new ClientCredentialsOptions { TokenUrl = "https://idp.example/token", ClientId = "svc", ClientSecret = "secret" };
        using var invoker = new HttpMessageInvoker(new ClientCredentialsHandler(cache, "orders", options) { InnerHandler = inner });
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://upstream.example/orders");

        // Act
        await invoker.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("Bearer cached-token", inner.Requests[0].Headers.Authorization!.ToString());
    }

    private static HttpContextAccessor AccessorWithCorrelationId(string correlationId)
    {
        var context = new RequestContext();
        context.Initialize(correlationId, "trace", null, null, null, null, null);

        var services = new ServiceCollection().AddSingleton(context).BuildServiceProvider();

        return new HttpContextAccessor { HttpContext = new DefaultHttpContext { RequestServices = services } };
    }
}
