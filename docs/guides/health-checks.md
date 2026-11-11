# Health checks

```csharp
services.AddAuroreHealthChecks(appInfo, settings.HealthChecks, settings.HttpDependencies);   // registers the checks
app.MapAuroreHealthChecks(appInfo);                                                          // maps the endpoints
```

## Endpoints

| Endpoint | Who | What it says |
|---|---|---|
| `/health/live` | Anonymous | The process is up (name and version). It never touches a dependency, so it is safe for frequent probes. |
| `/health/ready` | Anonymous | Whether the service can take traffic: only the checks tagged critical, as a status without details. For load balancers and orchestrators. |
| `/health/status` | Authenticated | Every dependency with its status and details. It reveals the names of the dependencies, so it needs a caller. |

Healthy and Degraded answer 200, Unhealthy answers 503. A dependency that is not critical is registered with a Degraded failure status, so its failure can
never make the service Unhealthy. The answers are cached for `HealthChecks:CacheSeconds` (5 by default, 0 turns it off), so a burst of probes
does not become a burst of calls to the dependencies.

## What is checked

- **HTTP dependencies:** a call to the readiness endpoint of each dependency that has a `HealthCheck` section ([HTTP dependencies](./http-dependencies.md)).
- **PostgreSQL:** `services.AddPostgresHealthCheck(options)` adds a `postgres` check (`SELECT 1`) tagged readiness, database and critical
  ([Entity Framework Core on Postgres](./entity-framework-postgres.md#health-check)).
- **Your own:** register an `IHealthCheck` with the tags of `HealthCheckProperties.Tags`: `Readiness` to be part of `/health/ready`, `Critical` to make its
  failure matter, `Liveness` for `/health/live`.
