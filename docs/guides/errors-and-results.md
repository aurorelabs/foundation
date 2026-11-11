# Errors, results and validation

## Result and ApplicationError

A use case returns a `Result` (no value) or `Result<T>`; an expected failure is an `ApplicationError`, not an exception.

```csharp
return person;                       // implicit: success carrying the value
return AppErrors.People.NotFound;    // implicit: failure
```

An error is declared once, as a static field, with a stable **code** (`area.snake_case_name`, what clients rely on), a
**message** (what a person reads, free to change) and a **kind**:

| Kind | HTTP |
|---|---|
| `Validation` | 400 |
| `Unauthorized` | 401 |
| `Forbidden` | 403 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `Failure` | 500 |
| `Unavailable` | 503 |

`StandardErrors` holds the ones every application shares; the application derives `AppErrors` from it and groups its own in nested
classes. `error.WithDetails(...)` returns a copy carrying per-occurrence details (field path to messages), leaving the shared
instance untouched. Reading `Value` of a failed result throws.

## Result to HTTP

`result.ToHttpResult()` answers 200 (or 204 without a value) on success and a problem on failure, with the message as
`detail`, the `code`, and the field `errors` when there are details. Use `ToHttpResult(success => TypedResults.Created(...))` to build the success response yourself.

## Where the rules live

Endpoints validate input shape and build the output; **business rules belong in the feature**.

| Validator | Base class | Run by | Fails with |
|---|---|---|---|
| Request shape | `RequestValidator<T>` | The HTTP validation filter, before the endpoint | 400 with the field errors |
| Business rule | `FeatureValidator<T>` | The `WithValidation` decorator of the feature pipeline | The error it declares (`Error` property; a 409 for a duplicate), else 400 |

Both derive from FluentValidation's `AbstractValidator<T>`. The same request type can have one of each. A plain `AbstractValidator`
(for example for settings) is not registered: create it where it is needed. Register the validators of an assembly with
`AddAuroreValidators`.

`failures.ToApplicationError()` turns FluentValidation failures into the standard validation error, with dotted camel case
paths (`items.0.name`); pass another error to keep its code and kind while attaching the field details.
