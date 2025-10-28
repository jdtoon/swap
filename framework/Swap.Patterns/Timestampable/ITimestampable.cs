namespace Swap.Patterns.Timestampable;

/// <summary>
/// Marks an entity as timestampable with automatic creation and update timestamps.
/// Simpler alternative to IAuditable when you don't need user tracking.
/// </summary>
public interface ITimestampable
{
    /// <summary>
    /// The date and time when the entity was created.
    /// Automatically set on insert.
    /// </summary>
    DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// The date and time when the entity was last updated.
    /// Automatically set on insert and update.
    /// </summary>
    DateTime UpdatedAt { get; set; }
}
