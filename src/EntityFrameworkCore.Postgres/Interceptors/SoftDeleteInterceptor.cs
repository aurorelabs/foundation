using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Interceptors;

/// <summary>
/// Turns the removal of an <see cref="ISoftDeletable"/> entity into an update of its <c>DeletedAt</c>, so Remove keeps the
/// row. Only that column is written. Related rows deleted by cascade are removed for real unless they are soft deletable
/// too. Deleting in bulk with ExecuteDelete does not go through here and removes the rows for real.
/// </summary>
/// <param name="timeProvider">The source of the current time.</param>
public sealed class SoftDeleteInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        MarkDeleted(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        MarkDeleted(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void MarkDeleted(DbContext? context)
    {
        if (context is null)
            return;

        var now = timeProvider.GetUtcNow();
        var removed = context.ChangeTracker.Entries<ISoftDeletable>().Where(x => x.State is EntityState.Deleted).ToList();

        foreach (var entry in removed)
        {
            entry.State = EntityState.Unchanged;
            entry.Property(x => x.DeletedAt).CurrentValue = now;
        }
    }
}
