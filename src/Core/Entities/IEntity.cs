namespace Aurore.Foundation.Core.Entities;

/// <summary>An entity identified by a <see cref="System.Guid"/>.</summary>
public interface IEntity : IEntity<System.Guid>;

/// <summary>An entity identified by a key of type <typeparamref name="TKey"/>.</summary>
/// <typeparam name="TKey">The type of the identifier.</typeparam>
public interface IEntity<TKey>
    where TKey : struct
{
    /// <summary>The unique identifier of the entity.</summary>
    public TKey Id { get; init; }
}
