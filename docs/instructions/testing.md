# Testing

## What we test

Our own logic, never the framework or a package: a test that would still pass if our code were deleted is not worth keeping.
Each test has explicit `// Arrange`, `// Act` and `// Assert` sections and a `DisplayName` that reads as a rule. Larger scenario sets use
`TheoryData` with `MemberData`, not long `InlineData` lists. Test folders mirror the source folders, shared stubs live in
`tests/Tests.Shared`, and `--filter-trait "Feature=Idempotency"` runs one feature.

| Project | What it holds |
|---|---|
| `tests/Tests.Core`, `Tests.AspNetCore`, `Tests.Telemetry` | Unit tests of the packages. |
| `tests/Tests.EntityFrameworkCore.Postgres` | Conventions, interceptors, unique violations, paging and the health check, **on a real Postgres**. |
| `tests/Tests.Shared` | Stubs shared by the test projects. It holds no tests. |

## The PostgreSQL the tests need

The Postgres tests run against a real server, because a fake would only prove that the fake works (how the sort maps and the keyset conditions
translate to SQL, the real unique violation). **They assume a PostgreSQL server is already there**: on `127.0.0.1:5432`, with the user and password `postgres`.
There is no container per project; every project of the machine shares that one server, and each test class creates a database of its own on it and drops it
afterwards, so nothing is left behind.

If you have no server (the sandbox of the machine, `.sandbox/docker-compose.yaml`, already has one), one is enough for everything (a restart policy keeps it up):

```
docker run -d --name postgres --restart unless-stopped -e POSTGRES_PASSWORD=postgres -p 5432:5432 -v postgres-data:/var/lib/postgresql postgres:18-alpine
```

Without a server these tests fail with a message that says so (they do not skip: a silent skip hides regressions). Use `127.0.0.1` and not `localhost` in any
client you write: `localhost` resolves to `::1` first, and docker in WSL does not forward IPv6, so a connection can hang before it falls back.

**Docker inside WSL on Windows:** WSL stops idle distributions after a few seconds, taking dockerd and the containers without a restart policy with it.

**Real tokens:** the authentication of the packages is also checked with real Keycloak tokens, but that test lives in the application that
uses them (the [blueprint](https://github.com/aurorelabs/blueprints)), because it needs a host and an identity provider configured the way an application does.

## Running

```
dotnet test --solution Foundation.slnx                                   # everything (needs the services above)
dotnet test --project tests/Tests.Core/Tests.Core.csproj
docker compose run --build --rm tests                                    # everything in a Linux container, with coverage
make test                                                                # the same; make test-local, make report (opens the coverage report), make clean
```

Results go to `artifacts/test-results`. The container run is the same environment as the pull request check (Linux, Release): it joins the `aur-sandbox` network and
reaches the server as `sbx-postgres` (`TEST_POSTGRES_HOST`). It writes the coverage report to `artifacts/coverage-report/index.html`.
