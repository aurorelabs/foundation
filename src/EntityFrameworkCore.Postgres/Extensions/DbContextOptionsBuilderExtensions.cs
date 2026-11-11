using Aurore.Foundation.EntityFrameworkCore.Postgres.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Extensions;

/// <summary>Options of the DbContext.</summary>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Names the columns and the key of the migrations history table in snake case, like the rest of the model. The history
    /// table is built apart from OnModelCreating, so the naming convention does not reach it.
    /// </summary>
    public static DbContextOptionsBuilder UseSnakeCaseNpgsqlHistoryTable(this DbContextOptionsBuilder optionsBuilder)
    {
        return optionsBuilder.ReplaceService<IHistoryRepository, SnakeCaseNpgsqlHistoryRepository>();
    }
}
