using System;
using System.Collections.Generic;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Models;

/// <summary>
/// The connection settings, written as the keywords Npgsql knows ("Host", "Port", "Database", "Username", "Password",
/// "SSL Mode", ...) so nothing is hidden behind a fixed shape. Keys are not case sensitive; an unknown keyword is
/// rejected when the connection string is built. Keep secrets out of the files: "Postgres__Password" in the environment.
/// </summary>
public sealed class PostgresOptions : Dictionary<string, string>
{
    /// <summary>Creates empty options.</summary>
    public PostgresOptions()
        : base(StringComparer.OrdinalIgnoreCase)
    {
    }
}
