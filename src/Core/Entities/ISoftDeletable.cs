using System;

namespace Aurore.Foundation.Core.Entities;

/// <summary>
/// An entity that is marked as deleted instead of removed. Removing it through the persistence layer sets
/// <see cref="DeletedAt"/>, and queries leave deleted entities out unless asked otherwise.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>When the entity was deleted, or null while it is active. Setting it back to null restores the entity.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
