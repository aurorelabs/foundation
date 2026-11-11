# Aurore Foundation

Reusable packages for services, packed for NuGet as `Aurore.Foundation.*` (the publishing pipeline is still to come). Nothing here depends on an application: the [blueprint](https://github.com/aurorelabs/blueprints)
is the reference service built on them.

| Package | What it is |
|---|---|
| `Core` | Results and errors, entities, features and decorators, pagination, privacy, validation, settings models, string and JSON helpers. No ASP.NET Core. |
| `AspNetCore` | Authentication, request pipeline, minimal endpoints and versioning, OpenAPI/Scalar, health checks, HTTP dependencies. |
| `Telemetry` | OpenTelemetry logs, metrics and traces over OTLP, with privacy of personal tags. |
| `EntityFrameworkCore.Postgres` | EF Core on PostgreSQL: conventions, soft delete, timestamps, unique violations, async paging, health check. |

## Naming

`Aurore` prefixes only the wiring (`AddAurore…`, `UseAurore…`, the namespaces). Helpers you use inside an endpoint or a feature have no
prefix. On the wire and in telemetry the short mark is `aur.` / `AUR-`.

## Documentation

The [documentation index](./docs/README.md) lists the guides (how to use each package) and the instructions (how to work on them). The guides:

- [Errors, results and validation](./docs/guides/errors-and-results.md)
- [Feature decorators](./docs/guides/feature-decorators.md)
- [Pagination](./docs/guides/pagination.md)
- [Entity Framework Core on Postgres](./docs/guides/entity-framework-postgres.md)
- [Authentication and authorization](./docs/guides/authentication.md)
- [Request pipeline](./docs/guides/request-pipeline.md)
- [Minimal endpoints](./docs/guides/minimal-endpoints.md)
- [API documentation (Scalar)](./docs/guides/scalar.md)
- [Telemetry](./docs/guides/telemetry.md)
- [Privacy of logs and traces](./docs/guides/privacy.md)
- [HTTP dependencies](./docs/guides/http-dependencies.md)
- [Health checks](./docs/guides/health-checks.md)
- [JSON serialization](./docs/guides/serialization.md)

To work on the packages: [testing](./docs/instructions/testing.md) and [changelog](./docs/instructions/changelog.md).

## Conventions

- Everything public has XML documentation (an error if missing) with `param`, `returns` and `exception` where they say something.
- Tests cover our own logic, never the framework or a package. Each package has a test project that mirrors its folders; shared
  stubs are in `tests/Tests.Shared`.
- No default behavior hides in a package: decorators, interceptors and filters act only where the application opted in, by interface,
  attribute or registration.
