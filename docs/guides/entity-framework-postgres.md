# Entity Framework Core on Postgres

`Aurore.Foundation.EntityFrameworkCore.Postgres` is EF Core on PostgreSQL by design: there is no provider abstraction and no
second package per database. It adds conventions, interceptors, async paging, a unique-violation decorator and a health check.

## Entities

Entities live in application code and use the interfaces and base classes of `Aurore.Foundation.Core.Entities`:

| Type | What you get |
|---|---|
| `IEntity` / `IEntity<TKey>` | An `Id`. |
| `ITimestamped` | `CreatedAt` and `UpdatedAt`, filled when the entity is saved. |
| `ISoftDeletable` | `DeletedAt`; a removed entity is kept and marked. |
| `Entity` | `IEntity` + `ITimestamped`. The id is a version 7 `Guid` created by the application, so it exists before the insert. |
| `SoftDeletableEntity` | `Entity` + `ISoftDeletable`. |

Everything below acts only on entities that implement the matching interface. An entity without it is untouched.

## Registering the context

```csharp
services.AddAuroreDbContext<AppDbContext>(appSettings.Postgres);
```

It registers the context with Npgsql, **queries that do not track entities** (load with `AsTracking()` to change what you
load), the two interceptors in the order they must run, and the snake case migrations history table. In the context:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder
        .ApplyConfigurationsFromAssembly(typeof(IInfra).Assembly)
        .ApplyAuroreConventions();   // last, after the configurations
}
```

If you register the context with a plain `AddDbContext` instead, the interceptors are not there.

## Conventions

`ApplyAuroreConventions()` runs two steps, which you can also call alone.

- **`ApplySoftDeleteFilters()`** adds a global query filter named `QueryFilters.SoftDelete` (`DeletedAt == null`) to every
  `ISoftDeletable` entity. `IgnoreQueryFilters()` shows deleted rows; `IgnoreQueryFilters([QueryFilters.SoftDelete])` ignores only this filter.
- **`UseSnakeCaseNamingConvention()`** writes schemas, tables, columns, keys, foreign keys and indexes in snake case.

`EntityConfigurationBase<TEntity>` is the base class of the configuration of an entity. It puts the table in the `app`
schema, names it after the entity in the plural (`Person` becomes `people`) and names the key `pk_people`. Override
`TableName` or `SchemaName` to change that, and `Extend` to configure the rest:

```csharp
internal sealed class PersonConfiguration : EntityConfigurationBase<Person>
{
    protected override void Extend(EntityTypeBuilder<Person> builder)
    {
        builder.Property(x => x.Email).HasMaxLength(384);
        builder.HasIndex(x => x.Email).IsUnique().WhereNotDeleted();
    }
}
```

`WhereNotDeleted()` makes the index partial (`"deleted_at" IS NULL`), so the value of a deleted row can be used again.
Column helpers: `AsTimestampColumn()` and `AsDecimalColumn()`.

## Interceptors

| Interceptor | Effect |
|---|---|
| `SoftDeleteInterceptor` | `Remove` on an `ISoftDeletable` entity becomes an update of `DeletedAt` only (so a change made meanwhile by someone else is not overwritten). Setting `DeletedAt` back to `null` restores the entity. |
| `TimestampInterceptor` | Sets `CreatedAt` and `UpdatedAt` on added entities and `UpdatedAt` on modified ones, from the `TimeProvider` (tests can fix the time). |

Soft delete runs first, so a deletion also moves `UpdatedAt`. Things that do **not** go through the interceptors:
`ExecuteDelete` and `ExecuteUpdate` (bulk statements) delete or update for real. Related rows removed by cascade are deleted for real
unless they are soft deletable too.

## Unique violations

When two requests create the same value at once, the database is what guarantees uniqueness: both can pass a check made
first. Enable the decorator and the second request gets a Conflict result instead of a 500:

```csharp
services.AddFeaturesFrom<IFeatures>(features => features.WithUniqueViolation());   // or [WithUniqueViolation] on one feature
```

Without any registration the answer is `database.conflict` (409) with the violated fields as details, read from the model
(`{ "email": ["A record with this value already exists."] }`); the name of the constraint is never sent. To answer with an
error of your own for one constraint, register it by the name of the index:

```csharp
services.AddUniqueViolationErrors(map => map.On("ux_people_email", PersonErrors.EmailTaken));
```

Checking first in the feature is still the way to give a friendlier message; it is not what guarantees uniqueness. Postgres
aborts the transaction on a violation, so a feature that keeps running statements in the same transaction must check first.

## Paging

`ToPageAsync` and `ToCursorPageAsync` are the asynchronous versions of `ToPage` and `ToCursorPage`, with the same rules; see
[Pagination](./pagination.md).

## Connection settings

`PostgresOptions` is a free-form map of the keywords Npgsql knows, so nothing is hidden behind a fixed shape:

```json
"Postgres": { "Host": "127.0.0.1", "Port": "5432", "Database": "app", "Username": "app", "Password": "<secret>" }
```

Keys are not case sensitive and an unknown keyword is rejected. `PostgresOptionsValidator` requires `Host`, `Database`,
`Username` and `Password`, and in production rejects a local host. Supply the password through the environment
(`Postgres__Password`), not a file.

## Health check

`services.AddPostgresHealthCheck(options)` adds a `postgres` check (`SELECT 1`) tagged readiness, database and critical.

## Migrations

Migrations live in a project of their own that only the EF tools use (for example `tools/Tools.Migrations`): a design-time
factory takes the connection string as the first argument after `--`, and the history table is `history.migrations`.

```
dotnet ef migrations add InitialDb --project tools/Tools.Migrations --output-dir History -- "<connection string>"
dotnet ef database update --project tools/Tools.Migrations -- "<connection string>"
```

## Testing

The tests of this package run on a real PostgreSQL server (the one on `127.0.0.1:5432`, shared with every project); see [testing](../instructions/testing.md).
