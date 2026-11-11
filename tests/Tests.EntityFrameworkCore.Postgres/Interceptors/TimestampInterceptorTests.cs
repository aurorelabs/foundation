using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Tests.EntityFrameworkCore.Postgres.Support;

namespace Tests.EntityFrameworkCore.Postgres.Interceptors;

[Trait("Feature", "Timestamps")]
public sealed class TimestampInterceptorTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact(DisplayName = "A saved new entity gets the creation and update time from the time provider")]
    public async Task AddedEntityIsStamped()
    {
        // Arrange
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 11, 10, 0, 0, TimeSpan.Zero));
        await using var provider = postgres.Provider(time: time);
        var id = await AddPersonAsync(provider, "added@example.com");

        // Act
        var stored = await FindAsync(provider, id);

        // Assert
        Assert.Equal(time.GetUtcNow(), stored.CreatedAt);
        Assert.Equal(time.GetUtcNow(), stored.UpdatedAt);
    }

    [Fact(DisplayName = "A modified entity gets a new update time and keeps its creation time")]
    public async Task ModifiedEntityMovesUpdatedAtOnly()
    {
        // Arrange
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 11, 10, 0, 0, TimeSpan.Zero));
        await using var provider = postgres.Provider(time: time);
        var created = time.GetUtcNow();
        var id = await AddPersonAsync(provider, "modified@example.com");

        time.Advance(TimeSpan.FromMinutes(5));

        // Act
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var person = await db.People.AsTracking().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
            person.Name = "Renamed";
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        var stored = await FindAsync(provider, id);
        Assert.Equal(created, stored.CreatedAt);
        Assert.Equal(created.AddMinutes(5), stored.UpdatedAt);
    }

    [Fact(DisplayName = "Saving an entity that did not change leaves its update time alone")]
    public async Task UnchangedEntityIsNotStamped()
    {
        // Arrange
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 11, 10, 0, 0, TimeSpan.Zero));
        await using var provider = postgres.Provider(time: time);
        var created = time.GetUtcNow();
        var id = await AddPersonAsync(provider, "unchanged@example.com");

        time.Advance(TimeSpan.FromMinutes(5));

        // Act
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.People.AsTracking().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        var stored = await FindAsync(provider, id);
        Assert.Equal(created, stored.UpdatedAt);
    }

    private static async Task<Guid> AddPersonAsync(IServiceProvider provider, string email)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var person = new Person { Name = "Ada", Email = email };

        db.People.Add(person);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return person.Id;
    }

    private static async Task<Person> FindAsync(IServiceProvider provider, Guid id)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        return await db.People.SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
    }
}
