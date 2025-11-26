using Swap.Htmx.Attributes;

namespace SwapLab.Views;

/// <summary>
/// Generated view path constants for the Patterns folder.
/// The [SwapViewSource] attribute triggers the source generator to scan
/// Views/Patterns for .cshtml files and generate constants.
/// </summary>
/// <remarks>
/// Usage in controller:
/// <code>
/// return this.SwapResponse()
///     .WithView(PatternViews.Partials.ProductGrid, model)
///     .Build();
/// </code>
/// 
/// Instead of magic strings:
/// <code>
/// return this.SwapResponse()
///     .WithView("_ProductGrid", model)  // Error prone!
///     .Build();
/// </code>
/// </remarks>
[SwapViewSource("Views/Patterns")]
public static partial class PatternViews { }

/// <summary>
/// Generated view path constants for the Home folder.
/// </summary>
[SwapViewSource("Views/Home")]
public static partial class HomeViews { }

/// <summary>
/// Generated view path constants for the Shared folder.
/// </summary>
[SwapViewSource("Views/Shared")]
public static partial class SharedViews { }

/// <summary>
/// Generated element ID constants for the Patterns folder.
/// The [SwapElementSource] attribute scans Views/Patterns/*.cshtml files
/// for id="..." attributes and generates strongly-typed constants.
/// </summary>
/// <remarks>
/// Usage in controller:
/// <code>
/// return this.SwapResponse()
///     .WithView("_Grid", model)
///     .AlsoUpdate(PatternIds.Pagination, "_Pagination", model)
///     .Build();
/// </code>
/// 
/// Instead of magic strings:
/// <code>
/// .AlsoUpdate("pagination", "_Pagination", model)  // Typo-prone!
/// </code>
/// </remarks>
[SwapElementSource("Views/Patterns")]
public static partial class PatternIds { }
