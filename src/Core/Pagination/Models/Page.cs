using System;
using System.Collections.Generic;
using System.Linq;

namespace Aurore.Foundation.Core.Pagination.Models;

/// <summary>One numbered page of a list, with the totals a client needs to build its pager.</summary>
/// <typeparam name="T">The type of item in the page.</typeparam>
public sealed record Page<T>
{
    /// <summary>The items of this page.</summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>The 1-based number of this page.</summary>
    public required int PageNumber { get; init; }

    /// <summary>The page size that was used.</summary>
    public required int PageSize { get; init; }

    /// <summary>How many items there are across all pages.</summary>
    public required int TotalItems { get; init; }

    /// <summary>How many pages there are. Zero when there are no items.</summary>
    public required int TotalPages { get; init; }

    /// <summary>Builds the page from the items already cut for it and the total count.</summary>
    /// <param name="items">The items already cut for this page.</param>
    /// <param name="pageNumber">The 1-based number of the page.</param>
    /// <param name="pageSize">The page size that was used.</param>
    /// <param name="totalItems">How many items there are across all pages.</param>
    public static Page<T> Create(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalItems)
    {
        return new Page<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
        };
    }

    /// <summary>The same page with each item transformed, for example from an entity to a response model.</summary>
    /// <param name="transform">Converts one item.</param>
    /// <returns>A page with the converted items and the same numbers.</returns>
    public Page<TResult> Map<TResult>(Func<T, TResult> transform)
    {
        return new Page<TResult>
        {
            Items = [.. Items.Select(transform)],
            PageNumber = PageNumber,
            PageSize = PageSize,
            TotalItems = TotalItems,
            TotalPages = TotalPages,
        };
    }
}
