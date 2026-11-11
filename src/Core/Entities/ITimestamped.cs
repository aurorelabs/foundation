using System;

namespace Aurore.Foundation.Core.Entities;

/// <summary>
/// An entity that records when it was created and last changed. The persistence layer fills both values when the entity is
/// saved, so application code never sets them.
/// </summary>
public interface ITimestamped
{
    /// <summary>When the entity was first saved.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the entity was last saved.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
