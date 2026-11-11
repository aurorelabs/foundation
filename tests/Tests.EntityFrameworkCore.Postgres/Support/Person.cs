using Aurore.Foundation.Core.Entities;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Configuration;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tests.EntityFrameworkCore.Postgres.Support;

/// <summary>A soft deletable entity with a unique email, used by the tests.</summary>
public sealed class Person : SoftDeletableEntity
{
    public required string Name { get; set; }
    public int Age { get; set; }
    public required string Email { get; set; }
}

/// <summary>An entity that is neither soft deletable nor given a configuration class, used to see the conventions leave it alone.</summary>
public sealed class Tag : Entity
{
    public required string Label { get; set; }
}

public sealed class PersonConfiguration : EntityConfigurationBase<Person>
{
    public const string UniqueEmailIndex = "ux_people_email";

    protected override void Extend(EntityTypeBuilder<Person> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(128);
        builder.Property(x => x.Email).HasMaxLength(384);
        builder.HasIndex(x => x.Email).IsUnique().WhereNotDeleted().HasDatabaseName(UniqueEmailIndex);
    }
}
