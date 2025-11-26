using System;

namespace Swap.Htmx.Attributes;

/// <summary>
/// Marks a partial class as a source for element ID constant generation.
/// The source generator will scan .cshtml files in the specified folder
/// for id="..." attributes and generate string constants for each unique ID.
/// </summary>
/// <remarks>
/// <para>
/// This eliminates magic strings when targeting elements for HTMX swaps,
/// providing compile-time safety and IntelliSense support.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// [SwapElementSource("Views/Inventory")]
/// public static partial class InventoryIds { }
/// 
/// // Generates (based on IDs found in .cshtml files):
/// public static partial class InventoryIds
/// {
///     public const string ProductGrid = "product-grid";
///     public const string Pagination = "pagination";
///     public const string SearchInput = "search-input";
/// }
/// </code>
/// </para>
/// <para>
/// Use in controllers:
/// <code>
/// return this.SwapResponse()
///     .WithView("_Grid", model)
///     .AlsoUpdate(InventoryIds.Pagination, "_Pagination", model)
///     .Build();
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SwapElementSourceAttribute : Attribute
{
    /// <summary>
    /// Gets the relative path to the views folder to scan for element IDs.
    /// </summary>
    public string ViewsPath { get; }

    /// <summary>
    /// Gets or sets whether to include subdirectories. Default is false.
    /// </summary>
    public bool IncludeSubdirectories { get; set; } = false;

    /// <summary>
    /// Gets or sets a prefix to filter IDs. Only IDs starting with this prefix will be included.
    /// Default is null (include all IDs).
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SwapElementSourceAttribute"/> class.
    /// </summary>
    /// <param name="viewsPath">
    /// The relative path to the views folder (e.g., "Views/Inventory").
    /// Path separators can be either / or \.
    /// </param>
    public SwapElementSourceAttribute(string viewsPath)
    {
        ViewsPath = viewsPath ?? throw new ArgumentNullException(nameof(viewsPath));
    }
}
