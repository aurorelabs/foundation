using System;
using Aurore.Foundation.Core.Entities;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Configuration;

/// <summary>
/// Base class for the configuration of entities identified by a <see cref="Guid"/>. See
/// <see cref="EntityConfigurationBase{TEntity, TKey}"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type being configured.</typeparam>
public abstract class EntityConfigurationBase<TEntity> : EntityConfigurationBase<TEntity, Guid>
    where TEntity : class, IEntity<Guid>;

/// <summary>
/// Base class for the configuration of an entity: puts it in the default schema, in a table named after the entity in
/// the plural, and names the primary key. Override <see cref="Extend"/> for everything else (properties, indexes,
/// relationships) and <see cref="TableName"/> or <see cref="SchemaName"/> to change where it lives.
/// </summary>
/// <typeparam name="TEntity">The entity type being configured.</typeparam>
/// <typeparam name="TKey">The type of the identifier of the entity.</typeparam>
public abstract class EntityConfigurationBase<TEntity, TKey> : IEntityTypeConfiguration<TEntity>
    where TEntity : class, IEntity<TKey>
    where TKey : struct
{
    /// <summary>The table name. Defaults to the entity name in the plural, without a trailing "Entity" ("Person" becomes "People").</summary>
    protected virtual string TableName { get; } = WithoutEntitySuffix(typeof(TEntity).Name).ToPlural();

    /// <summary>The schema of the table. Defaults to <see cref="DatabaseDefaults.Schema"/>.</summary>
    protected virtual string SchemaName { get; } = DatabaseDefaults.Schema;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.ToTable(TableName, SchemaName);

        builder
            .HasKey(x => x.Id)
            .HasName($"PK_{TableName}");

        Extend(builder);
    }

    /// <summary>Adds the configuration that is specific to the entity.</summary>
    /// <param name="builder">The builder of the entity type.</param>
    protected virtual void Extend(EntityTypeBuilder<TEntity> builder)
    {
    }

    private static string WithoutEntitySuffix(string name)
    {
        const string Suffix = "Entity";

        return name.Length > Suffix.Length && name.EndsWith(Suffix, StringComparison.Ordinal) ? name[..^Suffix.Length] : name;
    }
}
