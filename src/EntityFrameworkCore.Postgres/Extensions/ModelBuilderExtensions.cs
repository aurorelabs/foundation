using System;
using System.Linq;
using System.Linq.Expressions;
using Aurore.Foundation.Core.Entities;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Extensions;

/// <summary>Conventions applied to the whole model.</summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applies the conventions of the foundation, in the order they must run: the soft delete filter and then the snake
    /// case names. Call it last in OnModelCreating, after the entity configurations were applied.
    /// </summary>
    public static ModelBuilder ApplyAuroreConventions(this ModelBuilder modelBuilder)
    {
        return modelBuilder
            .ApplySoftDeleteFilters()
            .UseSnakeCaseNamingConvention();
    }

    /// <summary>
    /// Adds the <see cref="QueryFilters.SoftDelete"/> filter to every entity that implements <see cref="ISoftDeletable"/>,
    /// so queries leave deleted rows out. Use IgnoreQueryFilters to see them.
    /// </summary>
    public static ModelBuilder ApplySoftDeleteFilters(this ModelBuilder modelBuilder)
    {
        var roots = modelBuilder.Model.GetEntityTypes()
            .Where(x => x.BaseType is null && x.IsOwned() is false && typeof(ISoftDeletable).IsAssignableFrom(x.ClrType))
            .ToList();

        foreach (var entityType in roots)
        {
            // x => x.DeletedAt == null, built by hand because the entity type is only known at runtime.
            var parameter = Expression.Parameter(entityType.ClrType, "x");
            var deletedAt = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
            var notDeleted = Expression.Lambda(Expression.Equal(deletedAt, Expression.Constant(null, typeof(DateTimeOffset?))), parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(QueryFilters.SoftDelete, notDeleted);
        }

        return modelBuilder;
    }

    /// <summary>
    /// Writes the names of schemas, tables, columns, keys, foreign keys, indexes and the JSON names of complex properties
    /// in snake case ("OrderItems" becomes "order_items").
    /// </summary>
    public static ModelBuilder UseSnakeCaseNamingConvention(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.GetTableName()?.ToSnakeCase());
            entity.SetSchema(entity.GetSchema()?.ToSnakeCase());

            foreach (var key in entity.GetKeys())
                key.SetName(key.GetName()?.ToSnakeCase());

            foreach (var foreignKey in entity.GetForeignKeys())
                foreignKey.SetConstraintName(foreignKey.GetConstraintName()?.ToSnakeCase());

            foreach (var index in entity.GetIndexes())
                index.SetDatabaseName(index.GetDatabaseName()?.ToSnakeCase());

            var tableName = entity.GetTableName();

            if (tableName is not null)
                SnakeCaseColumns(entity, StoreObjectIdentifier.Table(tableName, entity.GetSchema()));
        }

        return modelBuilder;
    }

    // Properties nested in a complex property (Name.First) are not listed by GetProperties() of the entity: they live on the
    // complex type, and their default column name carries the prefix of the complex property, so it is read for the table.
    private static void SnakeCaseColumns(IMutableTypeBase typeBase, in StoreObjectIdentifier table)
    {
        foreach (var property in typeBase.GetProperties())
        {
            var columnName = property.GetColumnName(table);

            if (columnName is not null)
                property.SetColumnName(columnName.ToSnakeCase());
        }

        foreach (var complex in typeBase.GetComplexProperties())
        {
            complex.SetJsonPropertyName(complex.GetJsonPropertyName()?.ToSnakeCase());
            SnakeCaseColumns(complex.ComplexType, table);
        }
    }
}
