using Aurore.Foundation.Core.Entities;
using Aurore.Foundation.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Extensions;

/// <summary>Index shortcuts.</summary>
public static class IndexBuilderExtensions
{
    /// <summary>
    /// Limits the index to the rows that are not deleted (a partial index), so a unique index does not keep a deleted row's
    /// values from being used again. The filter names the column the snake case convention gives to DeletedAt.
    /// </summary>
    /// <typeparam name="TEntity">The soft deletable entity.</typeparam>
    /// <param name="index">The index being configured.</param>
    public static IndexBuilder<TEntity> WhereNotDeleted<TEntity>(this IndexBuilder<TEntity> index)
        where TEntity : ISoftDeletable
    {
        return index.HasFilter($"\"{nameof(ISoftDeletable.DeletedAt).ToSnakeCase()}\" IS NULL");
    }
}
