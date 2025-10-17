namespace NetMX.Ddd.Domain;

/// <summary>
/// Entities that implement this interface track when they were created.
/// </summary>
public interface IHasCreationTime
{
    /// <summary>
    /// Creation time of this entity.
    /// </summary>
    DateTime CreatedAt { get; set; }
}
