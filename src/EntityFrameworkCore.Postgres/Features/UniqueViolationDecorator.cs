using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.Core.Features;
using Aurore.Foundation.Core.Results;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Constants;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Features;

/// <summary>
/// Answers with a Conflict result when saving a feature's changes violates a unique constraint, instead of letting the
/// exception escape. The error is the one registered for the constraint in the <see cref="UniqueViolationMap"/>, if any;
/// otherwise it is <see cref="DatabaseErrors.Conflict"/> with the fields of the violated index or key as details, read from
/// the model, so nothing needs registering and the name of the constraint never reaches the client. Checking first would
/// cost a query on every call and still lose a race, so the database stays the judge. Postgres aborts the transaction on a
/// violation: a feature that goes on to run more statements inside the same transaction must check first instead.
/// </summary>
public sealed class UniqueViolationDecorator : IFeatureDecorator
{
    /// <inheritdoc />
    public async ValueTask<Result> InvokeAsync(FeatureContext context, FeatureDelegate next)
    {
        try
        {
            return await next();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresSqlStates.UniqueViolation } violation)
        {
            var map = context.Services.GetService<UniqueViolationMap>();

            return Result.Failure(map?.Find(violation.ConstraintName) ?? Describe(ex, violation.ConstraintName));
        }
    }

    // The failed entries know the model, and the model knows which properties the violated index or key covers.
    private static ApplicationError Describe(DbUpdateException exception, string? constraintName)
    {
        if (constraintName is null)
            return DatabaseErrors.Conflict;

        var fields = new HashSet<string>();

        foreach (var entityType in exception.Entries.Select(x => x.Metadata))
        {
            var indexed = entityType.GetIndexes().Where(x => x.GetDatabaseName() == constraintName).SelectMany(x => x.Properties);
            var keyed = entityType.GetKeys().Where(x => x.GetName() == constraintName).SelectMany(x => x.Properties);

            foreach (var property in indexed.Concat(keyed))
                fields.Add(property.Name.ToCamelCase());
        }

        return fields.Count == 0
            ? DatabaseErrors.Conflict
            : DatabaseErrors.Conflict.WithDetails(fields.ToDictionary(x => x, _ => new[] { "A record with this value already exists." }));
    }
}
