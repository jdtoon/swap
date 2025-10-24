namespace NetMX.Ddd.Domain.Entities;

/// <summary>
/// Base class for all domain entities with a strongly-typed identifier.
/// </summary>
/// <typeparam name="TKey">The type of the entity's identifier (e.g., Guid, int).</typeparam>
public abstract class Entity<TKey>
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public TKey Id { get; protected set; } = default!;
}