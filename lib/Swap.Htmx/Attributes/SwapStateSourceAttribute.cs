using System;

namespace Swap.Htmx.Attributes;

/// <summary>
/// Marks a partial class for automatic SwapState code generation from view annotations.
/// The generator scans .cshtml files for inputs with swap-state-prop attributes
/// and generates properties for the partial class.
/// </summary>
/// <remarks>
/// Usage in C#:
/// <code>
/// [SwapStateSource("Views/Inventory/_InventoryState.cshtml")]
/// public partial class InventoryState : SwapState { }
/// </code>
/// 
/// Usage in .cshtml:
/// <code>
/// &lt;input type="hidden" swap-state-prop="Tab:string=all" /&gt;
/// &lt;input type="hidden" swap-state-prop="Page:int=1" /&gt;
/// &lt;input type="hidden" swap-state-prop="Search:string?" /&gt;
/// </code>
/// 
/// Generates:
/// <code>
/// public partial class InventoryState
/// {
///     public string Tab { get; set; } = "all";
///     public int Page { get; set; } = 1;
///     public string? Search { get; set; }
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SwapStateSourceAttribute : Attribute
{
    /// <summary>
    /// The relative path to the .cshtml file containing state property definitions.
    /// </summary>
    public string ViewPath { get; }

    /// <summary>
    /// Creates a new SwapStateSourceAttribute.
    /// </summary>
    /// <param name="viewPath">The relative path to the .cshtml file (e.g., "Views/Inventory/_InventoryState.cshtml").</param>
    public SwapStateSourceAttribute(string viewPath)
    {
        ViewPath = viewPath ?? throw new ArgumentNullException(nameof(viewPath));
    }
}
