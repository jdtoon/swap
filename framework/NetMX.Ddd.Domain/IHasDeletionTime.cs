namespace NetMX.Ddd.Domain;

/// <summary>
/// Entities that implement this interface track when they were soft deleted.
/// </summary>
public interface IHasDeletionTime : ISoftDelete
{
    /// <summary>
    /// Deletion time of this entity.
    /// </summary>
    DateTime? DeletedAt { get; set; }
}
