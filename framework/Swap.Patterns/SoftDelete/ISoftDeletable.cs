namespace Swap.Patterns.SoftDelete;

/// <summary>
/// Interface for entities that support soft deletion.
/// Entities implementing this interface will not be permanently deleted from the database,
/// but marked as deleted instead.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Indicates whether the entity has been soft deleted.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// The date and time when the entity was soft deleted (UTC).
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Optional: The user or system that deleted the entity.
    /// </summary>
    string? DeletedBy { get; set; }
}
