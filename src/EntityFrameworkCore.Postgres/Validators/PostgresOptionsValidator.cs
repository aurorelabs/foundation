using System;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Extensions;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Models;
using FluentValidation;
using Npgsql;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Validators;

/// <summary>
/// Checks the connection settings: every keyword must be one Npgsql knows, Host, Database, Username and Password must be
/// present, and production rejects a local host.
/// </summary>
public sealed class PostgresOptionsValidator : AbstractValidator<PostgresOptions>
{
    /// <summary>Creates the validator. The flag turns on the stricter production rules.</summary>
    /// <param name="isProduction">True to reject a local host.</param>
    public PostgresOptionsValidator(bool isProduction)
    {
        RuleFor(x => x).Custom((options, context) =>
        {
            NpgsqlConnectionStringBuilder builder;

            try
            {
                builder = options.ToBuilder();
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException)
            {
                context.AddFailure(ex.Message);

                return;
            }

            if (builder.Host.HasValue() is false)
                context.AddFailure("Host", "Host is required.");

            if (builder.Database.HasValue() is false)
                context.AddFailure("Database", "Database is required.");

            if (builder.Username.HasValue() is false)
                context.AddFailure("Username", "Username is required.");

            if (builder.Password.HasValue() is false)
                context.AddFailure("Password", "Password is required. In deployed environments set it with the environment variable Postgres__Password.");

            if (isProduction && IsLocal(builder.Host))
                context.AddFailure("Host", $"'{builder.Host}' is a local address. Configure the production host in appsettings.Production.json or environment variables.");
        });
    }

    private static bool IsLocal(string? host)
    {
        return host is not null && (host.IsEqualTo("localhost") || host.StartsWith("127.", StringComparison.Ordinal) || host == "::1");
    }
}
