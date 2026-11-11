using System;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Extensions;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Interceptors;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Configuration;

/// <summary>Registers a DbContext on Postgres with the conventions of the foundation.</summary>
public static class DbContextConfiguration
{
    /// <summary>
    /// Registers the DbContext with: the Npgsql provider, queries that do not track entities (load with AsTracking() to
    /// change them), the soft delete and timestamp interceptors in the order they must run, and the snake case migrations
    /// history table. Call ApplyAuroreConventions in the OnModelCreating of the context for the model side.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The connection settings.</param>
    /// <param name="configure">Further configuration of the DbContext options, if needed.</param>
    public static IServiceCollection AddAuroreDbContext<TContext>(
        this IServiceCollection services,
        PostgresOptions options,
        Action<DbContextOptionsBuilder>? configure = null)
        where TContext : DbContext
    {
        var connectionString = options.ToConnectionString();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<SoftDeleteInterceptor>();
        services.TryAddSingleton<TimestampInterceptor>();

        return services.AddDbContext<TContext>((provider, builder) =>
        {
            builder
                .UseNpgsql(connectionString)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .UseSnakeCaseNpgsqlHistoryTable()
                .AddInterceptors(provider.GetRequiredService<SoftDeleteInterceptor>(), provider.GetRequiredService<TimestampInterceptor>());

            configure?.Invoke(builder);
        });
    }
}
