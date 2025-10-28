namespace Swap.Patterns.Auditable;

/// <summary>
/// Interface for entities that require automatic audit tracking of creation and modification.
/// </summary>
/// <remarks>
/// Implement this interface on entities to automatically track:
/// - When the entity was created (CreatedAt)
/// - Who created the entity (CreatedBy)
/// - When the entity was last updated (UpdatedAt)
/// - Who last updated the entity (UpdatedBy)
/// 
/// Use the AuditInterceptor to automatically populate these properties on SaveChanges.
/// </remarks>
public interface IAuditable
{
    /// <summary>
    /// The UTC timestamp when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// The identifier of the user who created the entity (e.g., user ID, email, username).
    /// </summary>
    string? CreatedBy { get; set; }

    /// <summary>
    /// The UTC timestamp when the entity was last updated.
    /// </summary>
    DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// The identifier of the user who last updated the entity (e.g., user ID, email, username).
    /// </summary>
    string? UpdatedBy { get; set; }
}
