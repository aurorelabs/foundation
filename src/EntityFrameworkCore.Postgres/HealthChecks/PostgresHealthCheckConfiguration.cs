using System;
using Aurore.Foundation.Core.HealthChecks.Constants;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Constants;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Extensions;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.HealthChecks;

/// <summary>Registers the Postgres health check.</summary>
public static class PostgresHealthCheckConfiguration
{
    /// <summary>
    /// Adds the "postgres" check (<c>SELECT 1</c>), tagged readiness, database and critical, with its own data source.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The connection settings.</param>
    public static IServiceCollection AddPostgresHealthCheck(this IServiceCollection services, PostgresOptions options)
    {
        string[] tags =
        [
            HealthCheckProperties.Tags.Readiness,
            HealthCheckProperties.Tags.TypeDatabase,
            HealthCheckProperties.Tags.Critical,
        ];

        var connectionString = options.ToConnectionString();

        services
            .AddKeyedSingleton(DatabaseDefaults.HealthCheckDataSourceKey, (_, _) => NpgsqlDataSource.Create(connectionString))
            .AddHealthChecks()
            .AddCheck<PostgresHealthCheck>(DatabaseDefaults.HealthCheckName, HealthStatus.Unhealthy, tags, TimeSpan.FromSeconds(HealthCheckProperties.Configuration.DefaultTimeoutSeconds));

        return services;
    }
}
