using System;
using System.Collections.Generic;
using Aurore.Foundation.Core.Errors;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Features;

/// <summary>
/// Says which error to answer with when a unique constraint is violated: the name of the constraint (the name of the unique
/// index, in snake case once the conventions ran) maps to the error. A constraint with no entry answers with
/// <see cref="Errors.DatabaseErrors.Conflict"/>, so the name of the constraint never reaches the client.
/// </summary>
public sealed class UniqueViolationMap
{
    private readonly Dictionary<string, ApplicationError> _errors = new(StringComparer.Ordinal);

    /// <summary>Answers a violation of the constraint with the error.</summary>
    /// <param name="constraintName">The name of the unique constraint or unique index in the database.</param>
    /// <param name="error">The error to answer with, usually a Conflict.</param>
    public UniqueViolationMap On(string constraintName, ApplicationError error)
    {
        _errors[constraintName] = error;

        return this;
    }

    internal ApplicationError? Find(string? constraintName)
    {
        return constraintName is not null && _errors.TryGetValue(constraintName, out var error) ? error : null;
    }
}
