using System;

namespace Swap.Htmx.Stories;

/// <summary>
/// Marks a controller action as a "Story" for the SwapStories component playground.
/// Stories render isolated components (partial views) for testing and visual verification.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class SwapStoryAttribute : Attribute
{
    /// <summary>
    /// gets the display title of the story.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the category/group name for the story (e.g., "Cards", "Forms").
    /// Stories are grouped by category in the dashboard sidebar.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Optional description to display in the dashboard.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Suggested viewport width for testing (default: 0 = fluid).
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Suggested viewport height for testing (default: 0 = fluid).
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Defines a story for the component playground.
    /// </summary>
    /// <param name="title">The name of the story (e.g. "Primary Button").</param>
    /// <param name="category">The group name (e.g. "Components").</param>
    public SwapStoryAttribute(string title, string category = "Components")
    {
        Title = title;
        Category = category;
    }
}
