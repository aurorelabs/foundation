using Aurore.Foundation.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations.Internal;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Migrations;

/// <summary>
/// The migrations history table with snake case columns and key. The table is built apart from OnModelCreating, so the
/// naming convention of the model does not reach it.
/// </summary>
/// <param name="dependencies">What the base history repository needs.</param>
#pragma warning disable EF1001 // NpgsqlHistoryRepository is Npgsql infrastructure; it is the only base type this customization can use.
public sealed class SnakeCaseNpgsqlHistoryRepository(HistoryRepositoryDependencies dependencies)
    : NpgsqlHistoryRepository(dependencies)
#pragma warning restore EF1001
{
    /// <inheritdoc />
    protected override void ConfigureTable(EntityTypeBuilder<HistoryRow> history)
    {
        base.ConfigureTable(history);

        history
            .Property(x => x.MigrationId)
            .HasColumnName(nameof(HistoryRow.MigrationId).ToSnakeCase());

        history
            .Property(x => x.ProductVersion)
            .HasColumnName(nameof(HistoryRow.ProductVersion).ToSnakeCase());

        history
            .HasKey(x => x.MigrationId)
            .HasName($"pk_{TableName}".ToSnakeCase());
    }
}
