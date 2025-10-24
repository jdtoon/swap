namespace NetMX.Ddd.Application.Dtos;

/// <summary>
/// Base class for Data Transfer Objects representing entities with a strongly-typed identifier.
/// </summary>
/// <typeparam name="TKey">The type of the entity's identifier.</typeparam>
public abstract class EntityDto<TKey>
{
    /// <summary>
    /// Gets or sets the unique identifier of the entity.
    /// </summary>
    public TKey Id { get; set; } = default!;
}