using System;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Features.Configuration;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Configuration;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Extensions;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Npgsql;

namespace Tests.EntityFrameworkCore.Postgres.Support;

/// <summary>
/// A database of its own on the PostgreSQL server of the machine (127.0.0.1:5432), created for the test class and dropped afterwards.
/// The server is shared by every project, and nothing of the tests is left on it. Fails with a hint when the server is not there.
/// </summary>
public class PostgresFixture : IAsyncLifetime
{
    private const string ServerHint = "The tests need a PostgreSQL server on 127.0.0.1:5432 with the user and password 'postgres' (see docs/instructions/testing.md).";

    private readonly string _database = $"aurore_{Guid.NewGuid():N}";

    /// <summary>The settings of the database created for this fixture.</summary>
    public PostgresOptions Options { get; } = new();

    /// <summary>The settings of the server's maintenance database.</summary>
    private static PostgresOptions Server(string database)
    {
        return new PostgresOptions
        {
            ["Host"] = Environment.GetEnvironmentVariable("TEST_POSTGRES_HOST") ?? "127.0.0.1", // not "localhost": it resolves to ::1 first, and docker in WSL does not forward IPv6
            ["Port"] = "5432",
            ["Database"] = database,
            ["Username"] = "postgres",
            ["Password"] = "postgres",
            ["Timeout"] = "5",
        };
    }

    /// <summary>The settings of a database that does not exist.</summary>
    public static PostgresOptions Unreachable()
    {
        var options = Server("postgres");
        options["Port"] = "55999";
        options["Timeout"] = "1";

        return options;
    }

    public async ValueTask InitializeAsync()
    {
        foreach (var (key, value) in Server(_database))
            Options[key] = value;

        try
        {
            await using var connection = new NpgsqlConnection(Server("postgres").ToConnectionString());
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_database}\"", connection);
            await create.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }
        catch (NpgsqlException ex)
        {
            throw new InvalidOperationException($"Could not reach the test PostgreSQL. {ServerHint}", ex);
        }

        await using var provider = Provider();
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        await SeedAsync(scope.ServiceProvider.GetRequiredService<TestDbContext>());
    }

    /// <summary>Adds the rows every test of the class starts from. Nothing by default.</summary>
    protected virtual ValueTask SeedAsync(TestDbContext db)
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();

        await using var connection = new NpgsqlConnection(Server("postgres").ToConnectionString());
        await connection.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", connection);
        await drop.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// A container with the DbContext of the foundation (interceptors included) on this database, a time that tests can
    /// move, and the sample features. Dispose it when done.
    /// </summary>
    public ServiceProvider Provider(Action<FeaturesBuilder>? features = null, Action<IServiceCollection>? register = null, FakeTimeProvider? time = null)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<TimeProvider>(time ?? new FakeTimeProvider(new DateTimeOffset(2026, 11, 11, 10, 0, 0, TimeSpan.Zero)))
            .AddAuroreDbContext<TestDbContext>(Options)
            .AddFeaturesFrom<Person>(features);

        register?.Invoke(services);

        return services.BuildServiceProvider();
    }
}
