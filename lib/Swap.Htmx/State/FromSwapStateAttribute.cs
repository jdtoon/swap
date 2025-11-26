using Microsoft.AspNetCore.Mvc;

namespace Swap.Htmx.State;

/// <summary>
/// Specifies that a parameter should be bound from SwapState hidden fields.
/// </summary>
/// <remarks>
/// Use this attribute to automatically bind state from hidden form fields:
/// <code>
/// public IActionResult Grid([FromSwapState] InventoryState state)
/// {
///     // state.Tab, state.Page, etc. are automatically populated
/// }
/// </code>
/// 
/// The binder looks for hidden fields matching the state property names.
/// Works with hx-include targeting a state container.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class FromSwapStateAttribute : ModelBinderAttribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="FromSwapStateAttribute"/>.
    /// </summary>
    public FromSwapStateAttribute() : base(typeof(SwapStateModelBinder))
    {
    }

    /// <summary>
    /// Gets or sets the name prefix for form fields.
    /// If not set, properties are bound without a prefix.
    /// </summary>
    /// <remarks>
    /// For example, if Prefix = "state", the binder will look for
    /// form fields named "state.Tab", "state.Page", etc.
    /// </remarks>
    public string? Prefix { get; set; }
}
