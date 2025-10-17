namespace NetMX.Ddd.Domain;

/// <summary>
/// Entities that implement this interface track when they were last modified.
/// </summary>
public interface IHasModificationTime
{
    /// <summary>
    /// Last modification time of this entity.
    /// </summary>
    DateTime? UpdatedAt { get; set; }
}
