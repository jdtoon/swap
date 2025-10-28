namespace Swap.Patterns.Publishable;

/// <summary>
/// Indicates an entity supports a draft/published workflow.
/// </summary>
public interface IPublishable
{
    /// <summary>
    /// True when the entity is published (visible to end users).
    /// </summary>
    bool IsPublished { get; set; }

    /// <summary>
    /// UTC timestamp when the entity was published; null when draft/unpublished.
    /// </summary>
    DateTime? PublishedAt { get; set; }
}
