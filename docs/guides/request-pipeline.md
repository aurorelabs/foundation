# Request pipeline

Registered in three steps; authentication stays in the application, in the middle:

```csharp
builder.AddAuroreRequestPipeline(options);   // registers everything

app.UseAuroreRequestPipeline(options);       // 1 forwarded headers, 2 request context, 3 exception handler,
                                             // 4 security headers, 5 compression, 6 CORS
app.UseAuthentication().UseAuthorization();  // the application
app.UseAuroreEndpointPolicies();             // 7 request user, 8 rate limiter, 9 timeouts, 10 output cache
```

The rate limiter comes after authentication because it partitions by user.

## Request context

`RequestContext` is filled once per request and cannot be overwritten. It carries the correlation id, trace and span ids,
idempotency key, client address, user agent, host and, after authentication, the user id.

- **Correlation id:** the `Correlation-Id` header, else `X-Request-ID`, else the trace id. A value must be 12 to 64 letters, digits,
  `.`, `_` or `-`; anything else is ignored. It is echoed on the response, tagged on the span (`aur.correlation.id`), put in the log scope
  and passed on to upstream calls by the HTTP dependencies.
- **Idempotency key:** 12 to 128 characters of the same set; an invalid one is refused with a 400.
- **Privacy** ([guide](./privacy.md)): with privacy on, personal values (client address, user agent, user id) are encrypted in logs and traces while the
  context keeps the real ones.

## Idempotency

Endpoints opt in with `.WithIdempotency()` (or `required: true` to answer 400 without a key). The same caller repeating the same key on the
same method and path gets the first successful result back with `Idempotency-Replayed`, without the endpoint running again;
failed results are not stored. It is best effort and in memory, kept for the configured duration (24 hours by default).

## Errors

An unhandled exception becomes a 500 problem without its message, with the correlation id. A malformed request keeps the status
the framework chose; a request the caller closed gets a 499. Empty 401, 403, 404 and 405 responses get a problem body.

## Security headers

`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY` and `Referrer-Policy: no-referrer` on every response. The content security policy and HSTS
apply in production only, so the API documentation can load while developing.

## Settings

All under `RequestPipeline` in `appsettings.json`: forwarded headers and trusted proxies, Kestrel limits, idempotency, CORS
policies, compression, rate limit policies, request timeouts and output cache policies, and `Privacy` (keys). The validators
reject values that cannot work, and in production the development defaults.
