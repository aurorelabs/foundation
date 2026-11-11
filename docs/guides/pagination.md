# Pagination

Two ways to list, both returning a `Result` so every invalid parameter comes back together as one 400 with the offending
parameters named.

| | Numbered (`ToPage`) | Cursor (`ToCursorPage`) |
|---|---|---|
| Request | `PageRequest`: `pageNumber`, `pageSize`, `orderBy` | `CursorRequest`: `cursor`, `pageSize`, `orderBy` |
| Response | `Page<T>`: `items`, `pageNumber`, `pageSize`, `totalItems`, `totalPages` | `CursorPage<T>`: `items`, `nextCursor`, `pageSize` |
| Cost | A count plus offset/limit; slower on deep pages | One keyset condition plus a limit; no count, same cost on every page |
| Jump to a page | Yes | No, forward only |
| Stable while rows change | No | Yes |
| Nullable sort field | Allowed | Not supported (a comparison with null never matches) |

Both come in a synchronous form for in-memory sources and an `Async` form for EF Core
([`ToPageAsync` and `ToCursorPageAsync`](./entity-framework-postgres.md#paging)), which share the same rules.

## Request classes

A request is a **class with `init` properties** that derives from one of the bases, because `[AsParameters]` binds inherited
properties only for classes (not positional records). The values are nullable so a missing one means "default".

```csharp
public sealed class SearchPeopleRequest : TimestampPageRequest   // PageRequest + createdFrom/To, updatedFrom/To
{
    public string? Name { get; init; }
}
```

`TimestampPageRequest` and `TimestampCursorRequest` add the creation and update ranges (`From` is inclusive, `To`
exclusive); apply them with `query.WhereTimestamps(request, x => x.CreatedAt, x => x.UpdatedAt)`.

## Choosing what clients can order by

`SortMap<T>` is the whitelist. A client can only order by a field that was allowed, in `orderBy=name:desc&orderBy=age`.

```csharp
private static readonly SortMap<PersonItem> _sort = SortMap.For<PersonItem>()
    .Allow(x => x.Name)                        // "name"
    .Allow(x => x.CreatedAt, "createdAt")
    .DefaultTo(x => x.Name, SortDirection.Asc); // used when the request asks for nothing; also allows the field
```

- A field is named after the property in camel case unless a name is given; a path through another object needs a name.
- `Id` is always appended as the last ordering unless the request already orders by it, so rows that tie keep a stable order.
  With no map, or a map without a default, the order is by `Id`; `ById(direction)` sets the direction. A type without `Id` needs a default of its own.
- Cursors need the `Id` too.

## Cursors

The cursor is opaque (base64url JSON of the last row's sort values) plus a fingerprint of the request without `cursor` and
`pageSize`. Sending it with other filters or another ordering is refused (`pagination.cursor_mismatch`); garbage is
`pagination.invalid_cursor`. The page size may change between calls.

## Settings

```json
"Pagination": { "DefaultPageSize": 20, "MaxPageSize": 100 }
```

A page size over the maximum is a 400, not a silent clamp. Register the options as a singleton and pass them to `ToPage`.

## Paging a projection

A list usually pages a projection (`Select` into a response type), because the response has fewer fields than the entity. The ordering is
applied after the projection, as `OrderBy(x => <projection>.Title)`, and **the database can only find a property of an object
initializer, not of a constructor call**. Declare the item with `init` properties and fill it with an initializer:

```csharp
public sealed record PostSummary { public required Guid Id { get; init; } public required string Title { get; init; } /* ... */ }

query.Select(x => new PostSummary { Id = x.Id, Title = x.Title /* ... */ })   // pages and orders fine
query.Select(x => new PostSummary(x.Id, x.Title))                              // ORDER BY cannot be translated
```

## In an endpoint

`.ProducesPage<T>()` and `.ProducesCursorPage<T>()` declare the 200 response of a list endpoint as a page of `T` (the 400 comes from the global problems). Keep the rules about
what may be listed in the feature, not in the endpoint.
