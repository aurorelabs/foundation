using System.Linq;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Tests.EntityFrameworkCore.Postgres.Support;

namespace Tests.EntityFrameworkCore.Postgres.Conventions;

[Trait("Feature", "Model conventions")]
public sealed class ModelConventionsTests
{
    public static TheoryData<string, string> PersonColumns => new()
    {
        { nameof(Person.Id), "id" },
        { nameof(Person.Name), "name" },
        { nameof(Person.CreatedAt), "created_at" },
        { nameof(Person.UpdatedAt), "updated_at" },
        { nameof(Person.DeletedAt), "deleted_at" },
    };

    [Fact(DisplayName = "An entity is put in the app schema, in a table named in the plural and in snake case")]
    public void TableIsInTheSchemaAndPlural()
    {
        // Arrange
        var model = BuildModel();

        // Act
        var person = model.FindEntityType(typeof(Person))!;

        // Assert
        Assert.Equal("people", person.GetTableName());
        Assert.Equal("app", person.GetSchema());
    }

    [Theory(DisplayName = "Columns are written in snake case")]
    [MemberData(nameof(PersonColumns))]
    public void ColumnsAreSnakeCase(string property, string expectedColumn)
    {
        // Arrange
        var model = BuildModel();
        var person = model.FindEntityType(typeof(Person))!;

        // Act
        var column = person.FindProperty(property)!.GetColumnName(StoreObjectIdentifier.Table("people", "app"));

        // Assert
        Assert.Equal(expectedColumn, column);
    }

    [Fact(DisplayName = "The primary key is named after the table, in snake case")]
    public void PrimaryKeyIsNamedAfterTheTable()
    {
        // Arrange
        var model = BuildModel();

        // Act
        var key = model.FindEntityType(typeof(Person))!.FindPrimaryKey()!;

        // Assert
        Assert.Equal("pk_people", key.GetName());
    }

    [Fact(DisplayName = "The soft delete filter is added to soft deletable entities only")]
    public void SoftDeleteFilterIsAddedToSoftDeletableEntitiesOnly()
    {
        // Arrange
        var model = BuildModel();

        // Act
        var onPerson = model.FindEntityType(typeof(Person))!.GetDeclaredQueryFilters().Select(x => x.Key);
        var onTag = model.FindEntityType(typeof(Tag))!.GetDeclaredQueryFilters();

        // Assert
        Assert.Contains(QueryFilters.SoftDelete, onPerson);
        Assert.Empty(onTag);
    }

    [Fact(DisplayName = "An entity with no configuration class is named by the conventions too")]
    public void EntityWithoutConfigurationIsSnakeCased()
    {
        // Arrange
        var model = BuildModel();

        // Act
        var tag = model.FindEntityType(typeof(Tag))!;

        // Assert
        Assert.Equal("tags", tag.GetTableName());
        Assert.Equal("created_at", tag.FindProperty(nameof(Tag.CreatedAt))!.GetColumnName());
        Assert.Equal("label", tag.FindProperty(nameof(Tag.Label))!.GetColumnName());
    }

    [Fact(DisplayName = "The unique index keeps only the rows that are not deleted")]
    public void UniqueIndexIsPartial()
    {
        // Arrange
        var model = BuildModel();

        // Act
        var index = model.FindEntityType(typeof(Person))!.GetIndexes().Single();

        // Assert
        Assert.True(index.IsUnique);
        Assert.Equal("\"deleted_at\" IS NULL", index.GetFilter());
        Assert.Equal(PersonConfiguration.UniqueEmailIndex, index.GetDatabaseName());
    }

    private static IModel BuildModel()
    {
        using var db = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseNpgsql("Host=localhost").Options);

        return db.GetService<IDesignTimeModel>().Model;
    }
}
