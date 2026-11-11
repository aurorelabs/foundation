using System.ComponentModel;

namespace Aurore.Foundation.Core.Pagination.Models;

/// <summary>
/// A list request that asks for one numbered page. Both values are optional: a missing page is the first one and a
/// missing page size is the DefaultPageSize of the pagination settings.
/// </summary>
public class PageRequest : SearchRequest
{
    /// <summary>The 1-based number of the page.</summary>
    [Description("The 1-based number of the page. The first page when left out.")]
    public int? PageNumber { get; init; }

    /// <summary>How many items a page holds, up to the MaxPageSize of the pagination settings.</summary>
    [Description("How many items a page holds, up to the maximum page size.")]
    public int? PageSize { get; init; }
}
