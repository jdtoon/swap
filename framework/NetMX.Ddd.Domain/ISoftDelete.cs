namespace NetMX.Ddd.Domain;

/// <summary>
/// Entities that implement this interface support soft deletion.
/// Soft deleted entities are not physically removed from the database.
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// Gets or sets whether this entity is soft deleted.
    /// </summary>
    bool IsDeleted { get; set; }
}