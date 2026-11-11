# Documentation

## Guides

How to use the packages, one topic per page.

| Guide | Package | About |
|---|---|---|
| [Errors, results and validation](./guides/errors-and-results.md) | Core | `Result`, `ApplicationError`, the two kinds of validators, result to HTTP. |
| [Feature decorators](./guides/feature-decorators.md) | Core | Features and the decorators that wrap them: telemetry, logging, validation, your own. |
| [Pagination](./guides/pagination.md) | Core, EntityFrameworkCore.Postgres | Numbered and cursor pages, `SortMap`, request classes. |
| [Entity Framework Core on Postgres](./guides/entity-framework-postgres.md) | EntityFrameworkCore.Postgres | Entities, conventions, soft delete, timestamps, unique violations, migrations. |
| [Authentication and authorization](./guides/authentication.md) | AspNetCore | JWT bearer, API keys, roles and scopes, pinning a scheme. |
| [Request pipeline](./guides/request-pipeline.md) | AspNetCore | Middleware order, request context, idempotency, errors, security headers. |
| [Minimal endpoints](./guides/minimal-endpoints.md) | AspNetCore | Resource groups, versions, conventions, startup validation. |
| [API documentation (Scalar)](./guides/scalar.md) | AspNetCore | One document per version, security schemes, sign-in from the page. |
| [Telemetry](./guides/telemetry.md) | Telemetry | Logs, metrics and traces over OTLP, custom metrics and sources. |
| [Privacy of logs and traces](./guides/privacy.md) | Core, AspNetCore, Telemetry | Encrypting personal data on its way out, keys and rotation. |
| [HTTP dependencies](./guides/http-dependencies.md) | AspNetCore | Clients for the services this one calls: correlation id, resilience, API key, client credentials. |
| [Health checks](./guides/health-checks.md) | AspNetCore, EntityFrameworkCore.Postgres | Live, ready and status endpoints, critical dependencies. |
| [JSON serialization](./guides/serialization.md) | Core | The single set of JSON rules. |

## Instructions

How to work on the packages themselves.

- [Testing](./instructions/testing.md): what we test, the services the tests need, how to run them.
- [Changelog](./instructions/changelog.md): what and how to write in `CHANGELOG.md`.
- [Publishing](./instructions/publishing.md): the version of a package, previews, what the pipelines check.
