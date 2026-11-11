using System;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.HealthChecks;

/// <summary>Checks that Postgres answers <c>SELECT 1</c> through the data source registered for the health check.</summary>
/// <param name="logger">Records why a check failed.</param>
/// <param name="serviceProvider">Resolves the data source.</param>
internal sealed class PostgresHealthCheck(ILogger<PostgresHealthCheck> logger, IServiceProvider serviceProvider) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var dataSource = serviceProvider.GetRequiredKeyedService<NpgsqlDataSource>(DatabaseDefaults.HealthCheckDataSourceKey);

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();

            command.CommandText = "SELECT 1;";

            var result = await command.ExecuteScalarAsync(cancellationToken);

            return result is 1
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Postgres is unavailable or unreachable.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not reach the Postgres instance.");

            return new HealthCheckResult(context.Registration.FailureStatus, "An error occurred while connecting to Postgres.", ex);
        }
    }
}
