using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.IO;
using System.Threading.Tasks;

namespace Swap.Htmx.Extensions;

/// <summary>
/// Extension methods for adding out-of-band swaps to view results in a fluent, type-safe manner.
/// </summary>
public static class SwapViewResultExtensions
{
    /// <summary>
    /// Adds an out-of-band swap to the view result, enabling multi-target page updates in a single response.
    /// This allows updating multiple DOM elements beyond the primary hx-target.
    /// </summary>
    /// <param name="viewResult">The view result to enhance with OOB swap.</param>
    /// <param name="targetId">The ID of the element to swap (must match an existing element ID in the DOM).</param>
    /// <param name="viewName">The partial view to render for this OOB swap.</param>
    /// <param name="model">The model to pass to the partial view.</param>
    /// <param name="swapMode">
    /// The swap strategy: "true" (default, outerHTML), "innerHTML", "beforebegin", "afterbegin", 
    /// "beforeend", "afterend", "delete", "none". Defaults to "true" which replaces the entire element.
    /// </param>
    /// <returns>The same view result for method chaining.</returns>
    /// <example>
    /// <code>
    /// [HttpPost("{id}/move")]
    /// public async Task&lt;IActionResult&gt; Move(int id, MoveDto dto)
    /// {
    ///     var task = await _service.MoveAsync(id, dto);
    ///     var allTasks = await _service.GetAllAsync();
    ///     
    ///     // Update main content + refresh count badge via OOB
    ///     return SwapView("_KanbanColumns", allTasks)
    ///         .WithOobSwap("task-count-badge", "_TaskCountBadge", allTasks.Count())
    ///         .WithOobSwap("notifications", "_Notifications", notifications);
    /// }
    /// </code>
    /// </example>
    public static ViewResult WithOobSwap(
        this ViewResult viewResult, 
        string targetId, 
        string viewName, 
        object? model = null,
        string swapMode = "true")
    {
        // Store OOB swap configuration in ViewData for rendering
        var oobKey = $"Oob_{targetId}_{Guid.NewGuid():N}";
        viewResult.ViewData[oobKey] = new OobSwapConfig
        {
            TargetId = targetId,
            ViewName = viewName,
            Model = model,
            SwapMode = swapMode
        };

        return viewResult;
    }

    /// <summary>
    /// Adds an out-of-band swap to a partial view result.
    /// </summary>
    public static PartialViewResult WithOobSwap(
        this PartialViewResult viewResult,
        string targetId,
        string viewName,
        object? model = null,
        string swapMode = "true")
    {
        // Store OOB swap configuration in ViewData for rendering
        var oobKey = $"Oob_{targetId}_{Guid.NewGuid():N}";
        viewResult.ViewData[oobKey] = new OobSwapConfig
        {
            TargetId = targetId,
            ViewName = viewName,
            Model = model,
            SwapMode = swapMode
        };

        return viewResult;
    }
}

/// <summary>
/// Configuration for an out-of-band swap.
/// </summary>
internal record OobSwapConfig
{
    public required string TargetId { get; init; }
    public required string ViewName { get; init; }
    public object? Model { get; init; }
    public string SwapMode { get; init; } = "true";
}
