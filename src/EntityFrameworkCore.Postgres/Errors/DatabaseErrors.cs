using Aurore.Foundation.Core.Errors;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Errors;

/// <summary>Errors the database layer produces.</summary>
public static class DatabaseErrors
{
    /// <summary>A unique constraint was violated and no specific error was registered for it.</summary>
    public static readonly ApplicationError Conflict =
        ApplicationError.Conflict("database.conflict", "The request conflicts with data that already exists.");
}
