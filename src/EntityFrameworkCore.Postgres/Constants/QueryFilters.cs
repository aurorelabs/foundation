namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Constants;

/// <summary>Names of the global query filters the conventions add, for IgnoreQueryFilters([...]).</summary>
public static class QueryFilters
{
    /// <summary>Leaves out the entities marked as deleted (ISoftDeletable).</summary>
    public const string SoftDelete = "soft-delete";
}
