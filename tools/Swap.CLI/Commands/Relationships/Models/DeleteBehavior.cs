namespace Swap.CLI.Commands.Relationships.Models;

/// <summary>
/// Defines what happens to related entities when the parent is deleted
/// </summary>
public enum DeleteBehavior
{
    /// <summary>
    /// Delete all related entities when parent is deleted
    /// Example: Delete all OrderItems when Order is deleted
    /// </summary>
    Cascade,

    /// <summary>
    /// Prevent deletion of parent if related entities exist
    /// Example: Cannot delete Customer if they have Orders
    /// </summary>
    Restrict,

    /// <summary>
    /// Set foreign key to NULL when parent is deleted
    /// Requires nullable foreign key
    /// Example: Set OrderId to NULL in OrderItem when Order is deleted
    /// </summary>
    SetNull
}
