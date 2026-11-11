using System.Threading.Tasks;
using Aurore.Foundation.EntityFrameworkCore.Postgres.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Tests.EntityFrameworkCore.Postgres.Support;

namespace Tests.EntityFrameworkCore.Postgres.HealthChecks;

[Trait("Feature", "Postgres health check")]
public sealed class PostgresHealthCheckTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact(DisplayName = "A reachable database is healthy")]
    public async Task ReachableDatabaseIsHealthy()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddPostgresHealthCheck(postgres.Options)
            .BuildServiceProvider();

        // Act
        var report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HealthStatus.Healthy, report.Status);
        Assert.Contains("postgres", report.Entries.Keys);
    }

    [Fact(DisplayName = "An unreachable database is unhealthy")]
    public async Task UnreachableDatabaseIsUnhealthy()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddPostgresHealthCheck(PostgresFixture.Unreachable())
            .BuildServiceProvider();

        // Act
        var report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, report.Status);
    }
}
