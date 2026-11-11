using Aurore.Foundation.EntityFrameworkCore.Postgres.Models;
using Npgsql;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Extensions;

/// <summary>Builds the connection string from the options.</summary>
public static class PostgresOptionsExtensions
{
    /// <summary>
    /// Builds the connection string with Npgsql's own builder, which knows the keywords and their aliases. Throws
    /// ArgumentException for an unknown keyword and FormatException for a value of the wrong type.
    /// </summary>
    public static string ToConnectionString(this PostgresOptions options)
    {
        return options.ToBuilder().ConnectionString;
    }

    internal static NpgsqlConnectionStringBuilder ToBuilder(this PostgresOptions options)
    {
        var builder = new NpgsqlConnectionStringBuilder();

        foreach (var (keyword, value) in options)
            builder[keyword] = value;

        return builder;
    }
}
