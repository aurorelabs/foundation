using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Pagination.Extensions;
using Aurore.Foundation.Core.Pagination.Models;
using Aurore.Foundation.Core.Results;
using Microsoft.EntityFrameworkCore;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Pagination;

/// <summary>
/// The asynchronous versions of ToPage and ToCursorPage for Entity Framework Core queries. They check the request with the
/// same rules, so the only difference is that the database is asked asynchronously.
/// </summary>
public static class PagingAsyncExtensions
{
    /// <summary>Orders, counts and cuts the query into the requested page. See <see cref="PagingExtensions.ToPage{T}"/>.</summary>
    public static async Task<Result<Page<T>>> ToPageAsync<T>(
        this IQueryable<T> query,
        PageRequest request,
        PaginationOptions options,
        SortMap<T>? sortMap = null,
        CancellationToken cancellationToken = default)
    {
        var plan = PagingExtensions.PlanPage(query, request, options, sortMap);

        if (plan.IsFailure)
            return plan.Error!;

        var total = await query.CountAsync(cancellationToken);
        var items = await plan.Value.Window.ToListAsync(cancellationToken);

        return Page<T>.Create(items, plan.Value.PageNumber, plan.Value.PageSize, total);
    }

    /// <summary>Orders the query and cuts the page after the cursor. See <see cref="PagingExtensions.ToCursorPage{T}"/>.</summary>
    public static async Task<Result<CursorPage<T>>> ToCursorPageAsync<T>(
        this IQueryable<T> query,
        CursorRequest request,
        PaginationOptions options,
        SortMap<T>? sortMap = null,
        CancellationToken cancellationToken = default)
    {
        var plan = PagingExtensions.PlanCursorPage(query, request, options, sortMap);

        if (plan.IsFailure)
            return plan.Error!;

        var rows = await plan.Value.Rows.ToListAsync(cancellationToken);

        return PagingExtensions.BuildCursorPage(rows, plan.Value);
    }
}
