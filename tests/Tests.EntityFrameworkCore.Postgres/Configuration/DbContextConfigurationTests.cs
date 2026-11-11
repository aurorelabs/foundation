using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.EntityFrameworkCore.Postgres.Support;

namespace Tests.EntityFrameworkCore.Postgres.Configuration;

[Trait("Feature", "DbContext registration")]
public sealed class DbContextConfigurationTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact(DisplayName = "Queries do not track entities unless the query asks for it")]
    public async Task QueriesDoNotTrackByDefault()
    {
        // Arrange
        await using var provider = postgres.Provider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        db.People.Add(new Person { Name = "Ada", Email = "tracking@example.com" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();

        // Act
        await db.People.ToListAsync(TestContext.Current.CancellationToken);
        var untracked = db.ChangeTracker.Entries().Count();
        await db.People.AsTracking().ToListAsync(TestContext.Current.CancellationToken);
        var tracked = db.ChangeTracker.Entries().Count();

        // Assert
        Assert.Equal(0, untracked);
        Assert.True(tracked > 0);
    }
}
