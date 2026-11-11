using System;
using System.Threading.Tasks;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Tests.EntityFrameworkCore.Postgres.Support;

namespace Tests.EntityFrameworkCore.Postgres.Interceptors;

[Trait("Feature", "Soft delete")]
public sealed class SoftDeleteInterceptorTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact(DisplayName = "Removing a soft deletable entity keeps the row and marks it as deleted at the current time")]
    public async Task RemoveKeepsTheRow()
    {
        // Arrange
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 11, 10, 0, 0, TimeSpan.Zero));
        await using var provider = postgres.Provider(time: time);
        var id = await AddAsync(provider, "removed@example.com");

        time.Advance(TimeSpan.FromHours(1));

        // Act
        await RemoveAsync(provider, id);

        // Assert
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var stored = await db.People.IgnoreQueryFilters().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        Assert.Equal(time.GetUtcNow(), stored.DeletedAt);
        Assert.Equal(time.GetUtcNow(), stored.UpdatedAt);
    }

    [Fact(DisplayName = "Queries leave deleted entities out unless the filter is ignored")]
    public async Task DeletedEntitiesAreFilteredOut()
    {
        // Arrange
        await using var provider = postgres.Provider();
        var id = await AddAsync(provider, "filtered@example.com");
        await RemoveAsync(provider, id);

        // Act
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var visible = await db.People.AnyAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        var all = await db.People.IgnoreQueryFilters([QueryFilters.SoftDelete]).AnyAsync(x => x.Id == id, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(visible);
        Assert.True(all);
    }

    [Fact(DisplayName = "Only the deletion is written, so a change made meanwhile by someone else is kept")]
    public async Task OnlyDeletedAtIsWritten()
    {
        // Arrange
        await using var provider = postgres.Provider();
        var id = await AddAsync(provider, "partial@example.com");

        await using var first = provider.CreateAsyncScope();
        var firstDb = first.ServiceProvider.GetRequiredService<TestDbContext>();
        var loaded = await firstDb.People.AsTracking().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);

        await using (var second = provider.CreateAsyncScope())
        {
            var secondDb = second.ServiceProvider.GetRequiredService<TestDbContext>();
            var other = await secondDb.People.AsTracking().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
            other.Name = "Changed by someone else";
            await secondDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        firstDb.People.Remove(loaded);
        await firstDb.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var scope = provider.CreateAsyncScope();
        var stored = await scope.ServiceProvider.GetRequiredService<TestDbContext>().People
            .IgnoreQueryFilters().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        Assert.Equal("Changed by someone else", stored.Name);
        Assert.NotNull(stored.DeletedAt);
    }

    [Fact(DisplayName = "Clearing DeletedAt restores the entity")]
    public async Task ClearingDeletedAtRestores()
    {
        // Arrange
        await using var provider = postgres.Provider();
        var id = await AddAsync(provider, "restored@example.com");
        await RemoveAsync(provider, id);

        // Act
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var deleted = await db.People.IgnoreQueryFilters().AsTracking().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
            deleted.DeletedAt = null;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        await using var check = provider.CreateAsyncScope();
        var visible = await check.ServiceProvider.GetRequiredService<TestDbContext>().People.AnyAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        Assert.True(visible);
    }

    [Fact(DisplayName = "An entity that is not soft deletable is removed for real")]
    public async Task OtherEntitiesAreRemovedForReal()
    {
        // Arrange
        await using var provider = postgres.Provider();
        Guid id;

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var tag = new Tag { Label = "temporary" };
            db.Tags.Add(tag);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            id = tag.Id;
        }

        // Act
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var tag = await db.Tags.AsTracking().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
            db.Tags.Remove(tag);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        await using var check = provider.CreateAsyncScope();
        var exists = await check.ServiceProvider.GetRequiredService<TestDbContext>().Tags.AnyAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        Assert.False(exists);
    }

    [Fact(DisplayName = "The email of a deleted entity can be used again, while two active ones still cannot share it")]
    public async Task UniqueIndexIgnoresDeletedRows()
    {
        // Arrange
        await using var provider = postgres.Provider();
        var first = await AddAsync(provider, "shared@example.com");
        await RemoveAsync(provider, first);

        // Act
        var second = await AddAsync(provider, "shared@example.com");
        var duplicate = await Record.ExceptionAsync(() => AddAsync(provider, "shared@example.com"));

        // Assert
        Assert.NotEqual(first, second);
        var violation = Assert.IsType<DbUpdateException>(duplicate);
        Assert.Equal(PostgresSqlStates.UniqueViolation, Assert.IsType<PostgresException>(violation.InnerException).SqlState);
    }

    private static async Task<Guid> AddAsync(IServiceProvider provider, string email)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var person = new Person { Name = "Ada", Email = email };

        db.People.Add(person);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return person.Id;
    }

    private static async Task RemoveAsync(IServiceProvider provider, Guid id)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var person = await db.People.AsTracking().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);

        db.People.Remove(person);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
