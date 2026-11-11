# HTTP dependencies

The services this one calls. Each is declared once in the settings and registered once in code, and the client you get already carries the
base address, the correlation id of the current request, the standard resilience pipeline and, when asked, its authentication.

```csharp
services.AddAuroreHttpDependency("billing", settings.HttpDependencies["billing"]);            // named client
services.AddAuroreHttpDependency<IBillingClient, BillingClient>("billing", settings.HttpDependencies["billing"]); // typed client
```

Resolve a named client with `IHttpClientFactory.CreateClient("billing")`; a typed one is injected as its interface.

## Settings

```json
"HttpDependencies": {
  "billing": {
    "BaseUrl": "https://billing.internal.example.com",
    "RequestHeaders": { "Accept": "application/json" },
    "PropagateCorrelationId": true,
    "ClientCredentials": { "TokenUrl": "https://auth.example.com/realms/x/protocol/openid-connect/token", "ClientId": "svc", "ClientSecret": "<secret>", "Scopes": ["billing.read"] },
    "HealthCheck": { "Critical": true }
  }
}
```

The validators reject a relative base URL and, in production, a local one (`localhost`). Secrets come from the environment
(`HttpDependencies__billing__ClientCredentials__ClientSecret`).

## What every client gets

- The base address and the default headers.
- **The correlation id** of the current request in the `Correlation-Id` header (unless the call already sets one), so one request can be followed
  through every service. Turn it off with `PropagateCorrelationId: false`.
- **The standard resilience pipeline** of Microsoft.Extensions.Http.Resilience: retries with backoff, a circuit breaker and timeouts.

## Authenticating the calls

Chain one after the registration; without any the client is anonymous:

| Call | What it does |
|---|---|
| `.AddAuroreApiKey(options)` | Sends a static header on every call (`X-Api-Key` by default; a different `HeaderName` and the `Value`). |
| `.AddAuroreClientCredentials(options)` | The OAuth2 client credentials grant: asks the token endpoint for a token with the client and scopes, keeps it per dependency, and sends it as `Authorization: Bearer`. |
| `.AddHttpMessageHandler<T>()` | A handler of your own, for anything else (signed requests, for example). |

The token cache asks for a new token a minute before the old one expires (at half of its life for tokens that live less than two minutes), lets one
request at a time ask for the same dependency, and does not keep a failure: the next call asks again.

## Health

A dependency with a `HealthCheck` section also gets a check in the readiness report: it calls the readiness endpoint of the dependency (`/health/ready`
unless `ReadinessEndpoint` says otherwise) with its own timeout (5 seconds by default). `Critical: true` makes the service not ready when the dependency is down;
otherwise the failure only degrades it. See [health checks](./health-checks.md).
