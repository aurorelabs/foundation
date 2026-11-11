# Changelog

All notable changes to this project will be documented in this file.

Read [the instructions file](./docs/instructions/changelog.md) to learn what and how to write here.

## [Unreleased]

### Added

- `Aurore.Foundation.Core`: results and errors that map to HTTP, entities with timestamps and soft delete, features with opt-in decorators (telemetry, logging, validation), numbered and cursor pagination with a whitelist of sortable fields, and encryption of personal data in logs and traces.
- `Aurore.Foundation.AspNetCore`: JWT bearer and API key authentication with provider-independent roles and scopes, the request pipeline (correlation id, idempotency, security headers, rate limiting), minimal endpoints with versions, OpenAPI documents with Scalar, health checks and HTTP dependencies with resilience and OAuth2 client credentials.
- `Aurore.Foundation.Telemetry`: OpenTelemetry logs, metrics and traces over OTLP.
- `Aurore.Foundation.EntityFrameworkCore.Postgres`: conventions (snake case, schema, soft delete filter), timestamp and soft delete interceptors, a decorator that answers unique violations with a conflict, async paging and a health check.

---
