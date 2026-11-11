using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.HttpDependencies.Services;
using Aurore.Foundation.Core.HttpDependencies.Models;
using Microsoft.Extensions.Time.Testing;
using Tests.AspNetCore.Support;

namespace Tests.AspNetCore.HttpDependencies.Services;

[Trait("Feature", "Client credentials token cache")]
public sealed class ClientCredentialsTokenCacheTests
{
    private static readonly ClientCredentialsOptions _options = new()
    {
        TokenUrl = "https://idp.example/token",
        ClientId = "svc",
        ClientSecret = "secret",
        Scopes = ["orders.read", "orders.write"],
    };

    [Fact(DisplayName = "A token is reused until it is about to expire")]
    public async Task TokenIsReusedUntilItIsAboutToExpire()
    {
        // Arrange
        var (cache, handler, time) = Create(expiresIn: 300);
        var first = await cache.GetTokenAsync("orders", _options, TestContext.Current.CancellationToken);

        // Act
        time.Advance(TimeSpan.FromSeconds(200));
        var second = await cache.GetTokenAsync("orders", _options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(1, handler.Calls);
    }

    [Fact(DisplayName = "A new token is requested a minute before the old one expires")]
    public async Task TokenIsRefreshedBeforeItExpires()
    {
        // Arrange
        var (cache, handler, time) = Create(expiresIn: 300);
        var first = await cache.GetTokenAsync("orders", _options, TestContext.Current.CancellationToken);

        // Act
        time.Advance(TimeSpan.FromSeconds(241));
        var second = await cache.GetTokenAsync("orders", _options, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEqual(first, second);
        Assert.Equal(2, handler.Calls);
    }

    [Fact(DisplayName = "A short-lived token is refreshed at half of its life instead of at once")]
    public async Task ShortLivedTokenIsRefreshedAtHalfItsLife()
    {
        // Arrange
        var (cache, handler, time) = Create(expiresIn: 100);
        await cache.GetTokenAsync("orders", _options, TestContext.Current.CancellationToken);

        // Act
        time.Advance(TimeSpan.FromSeconds(49));
        await cache.GetTokenAsync("orders", _options, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(2));
        await cache.GetTokenAsync("orders", _options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, handler.Calls);
    }

    [Fact(DisplayName = "Callers that arrive together share one token request")]
    public async Task ConcurrentCallersShareOneRequest()
    {
        // Arrange
        var (cache, handler, _) = Create(expiresIn: 300, delay: TimeSpan.FromMilliseconds(200));

        // Act
        var tokens = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => cache.GetTokenAsync("orders", _options, TestContext.Current.CancellationToken).AsTask()));

        // Assert
        Assert.Equal(1, handler.Calls);
        Assert.Single(tokens.Distinct());
    }

    [Fact(DisplayName = "Each dependency has its own token")]
    public async Task EachDependencyHasItsOwnToken()
    {
        // Arrange
        var (cache, handler, _) = Create(expiresIn: 300);

        // Act
        var orders = await cache.GetTokenAsync("orders", _options, TestContext.Current.CancellationToken);
        var inventory = await cache.GetTokenAsync("inventory", _options, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEqual(orders, inventory);
        Assert.Equal(2, handler.Calls);
    }

    [Fact(DisplayName = "The request asks for the client credentials grant with the client and the scopes")]
    public async Task RequestCarriesTheGrantTheClientAndTheScopes()
    {
        // Arrange
        var (cache, handler, _) = Create(expiresIn: 300);

        // Act
        await cache.GetTokenAsync("orders", _options, TestContext.Current.CancellationToken);

        // Assert
        var body = Assert.Single(handler.Bodies);
        Assert.Contains("grant_type=client_credentials", body);
        Assert.Contains("client_id=svc", body);
        Assert.Contains("client_secret=secret", body);
        Assert.Contains("scope=orders.read+orders.write", body);
        Assert.Equal("https://idp.example/token", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact(DisplayName = "A refused request throws with the status and is not cached, so the next call asks again")]
    public async Task FailureThrowsAndIsNotCached()
    {
        // Arrange
        var failing = true;
        var handler = new StubHandler(_ => failing ? new HttpResponseMessage(HttpStatusCode.Unauthorized) : Token("t1", 300));
        var cache = new ClientCredentialsTokenCache(new StubClientFactory(handler), new FakeTimeProvider());

        // Act
        var thrown = await Assert.ThrowsAsync<HttpRequestException>(async () => await cache.GetTokenAsync("orders", _options, TestContext.Current.CancellationToken));
        failing = false;
        var token = await cache.GetTokenAsync("orders", _options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, thrown.StatusCode);
        Assert.Equal("t1", token);
    }

    [Fact(DisplayName = "An answer with no access token throws")]
    public async Task AnswerWithoutAnAccessTokenThrows()
    {
        // Arrange
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"expires_in":300}""", Encoding.UTF8, "application/json") });
        var cache = new ClientCredentialsTokenCache(new StubClientFactory(handler), new FakeTimeProvider());

        // Act
        var thrown = await Record.ExceptionAsync(async () => await cache.GetTokenAsync("orders", _options, TestContext.Current.CancellationToken));

        // Assert
        Assert.IsType<HttpRequestException>(thrown);
    }

    private static (ClientCredentialsTokenCache Cache, StubHandler Handler, FakeTimeProvider Time) Create(int expiresIn, TimeSpan? delay = null)
    {
        var count = 0;
        var handler = new StubHandler(_ => Token($"token-{Interlocked.Increment(ref count)}", expiresIn), delay);
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 11, 10, 0, 0, TimeSpan.Zero));

        return (new ClientCredentialsTokenCache(new StubClientFactory(handler), time), handler, time);
    }

    private static HttpResponseMessage Token(string accessToken, int expiresIn)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($$"""{"access_token":"{{accessToken}}","expires_in":{{expiresIn}}}""", Encoding.UTF8, "application/json"),
        };
    }
}
