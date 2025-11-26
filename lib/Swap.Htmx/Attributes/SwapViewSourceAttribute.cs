using System;

namespace Swap.Htmx.Attributes;

/// <summary>
/// Marks a partial class as a source for view path constant generation.
/// The source generator will scan the specified folder for .cshtml files
/// and generate string constants for each view name.
/// </summary>
/// <remarks>
/// <para>
/// Views starting with underscore (_) are considered partials and are
/// generated in a nested <c>Partials</c> class.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// [SwapViewSource("Views/Inventory")]
/// public static partial class InventoryViews { }
/// 
/// // Generates:
/// public static partial class InventoryViews
/// {
///     public const string Index = "Index";
///     public const string Edit = "Edit";
///     public static class Partials
///     {
///         public const string Grid = "_Grid";
///         public const string Pagination = "_Pagination";
///     }
/// }
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SwapViewSourceAttribute : Attribute
{
    /// <summary>
    /// Gets the relative path to the views folder to scan.
    /// </summary>
    public string ViewsPath { get; }

    /// <summary>
    /// Gets or sets whether to include subdirectories. Default is false.
    /// </summary>
    public bool IncludeSubdirectories { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="SwapViewSourceAttribute"/> class.
    /// </summary>
    /// <param name="viewsPath">
    /// The relative path to the views folder (e.g., "Views/Inventory").
    /// Path separators can be either / or \.
    /// </param>
    public SwapViewSourceAttribute(string viewsPath)
    {
        ViewsPath = viewsPath ?? throw new ArgumentNullException(nameof(viewsPath));
    }
}
