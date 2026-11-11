namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Constants;

/// <summary>The PostgreSQL error codes (SQLSTATE) the foundation reacts to.</summary>
public static class PostgresSqlStates
{
    /// <summary>A unique constraint or unique index was violated.</summary>
    public const string UniqueViolation = "23505";
}
