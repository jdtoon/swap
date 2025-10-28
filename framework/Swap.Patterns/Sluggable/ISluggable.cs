namespace Swap.Patterns.Sluggable;

/// <summary>
/// Interface for entities that require SEO-friendly URL slugs.
/// </summary>
/// <remarks>
/// Implement this interface on entities to enable automatic slug generation
/// from a source property (typically Title or Name). Slugs are URL-safe strings
/// like "my-blog-post" instead of IDs like "123".
/// </remarks>
public interface ISluggable
{
    /// <summary>
    /// The URL-friendly slug for this entity.
    /// </summary>
    /// <remarks>
    /// Should be unique per entity type and URL-safe (lowercase, hyphens, no spaces).
    /// Example: "my-awesome-blog-post"
    /// </remarks>
    string Slug { get; set; }
}
