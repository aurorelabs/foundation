using System;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Interceptors;

/// <summary>
/// Stamps <see cref="ITimestamped"/> entities when they are saved: added ones get <c>CreatedAt</c> and <c>UpdatedAt</c>,
/// modified ones get <c>UpdatedAt</c>. The time comes from the <see cref="TimeProvider"/>, so tests can fix it. Register it
/// after <see cref="SoftDeleteInterceptor"/>, so a soft delete also moves <c>UpdatedAt</c>.
/// </summary>
/// <param name="timeProvider">The source of the current time.</param>
public sealed class TimestampInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
            return;

        var now = timeProvider.GetUtcNow();

        foreach (var entry in context.ChangeTracker.Entries<ITimestamped>())
        {
            if (entry.State is EntityState.Added)
            {
                entry.Property(x => x.CreatedAt).CurrentValue = now;
                entry.Property(x => x.UpdatedAt).CurrentValue = now;
            }
            else if (entry.State is EntityState.Modified)
            {
                entry.Property(x => x.UpdatedAt).CurrentValue = now;
            }
        }
    }
}
