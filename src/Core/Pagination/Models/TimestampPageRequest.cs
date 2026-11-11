using System;
using System.ComponentModel;

namespace Aurore.Foundation.Core.Pagination.Models;

/// <summary>A page request that can also filter by the creation and update times. See <see cref="ITimestampFilter"/>.</summary>
public class TimestampPageRequest : PageRequest, ITimestampFilter
{
    /// <inheritdoc />
    [Description("Items created at or after this time.")]
    public DateTimeOffset? CreatedFrom { get; init; }

    /// <inheritdoc />
    [Description("Items created before this time.")]
    public DateTimeOffset? CreatedTo { get; init; }

    /// <inheritdoc />
    [Description("Items updated at or after this time.")]
    public DateTimeOffset? UpdatedFrom { get; init; }

    /// <inheritdoc />
    [Description("Items updated before this time.")]
    public DateTimeOffset? UpdatedTo { get; init; }
}
