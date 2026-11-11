namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Constants;

/// <summary>Defaults the conventions use.</summary>
public static class DatabaseDefaults
{
    /// <summary>The schema the tables are created in, before the snake case convention is applied.</summary>
    public const string Schema = "app";

    /// <summary>The service key of the data source the Postgres health check uses.</summary>
    public const string HealthCheckDataSourceKey = "hc-postgres";

    /// <summary>The name of the Postgres health check.</summary>
    public const string HealthCheckName = "postgres";
}
