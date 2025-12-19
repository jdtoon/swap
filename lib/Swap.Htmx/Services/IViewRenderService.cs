using Microsoft.AspNetCore.Mvc;

namespace Swap.Htmx.Services;

/// <summary>
/// Service for rendering Razor views to strings in a consistent way.
/// Provides centralized view search logic and diagnostic messages.
/// </summary>
public interface IViewRenderService
{
    /// <summary>
    /// Renders a partial view to a string.
    /// </summary>
    /// <param name="viewName">The name of the view to render.</param>
    /// <param name="model">The model to pass to the view.</param>
    /// <param name="controller">Optional controller for context (when available).</param>
    /// <returns>The rendered HTML string.</returns>
    Task<string> RenderPartialToStringAsync<TModel>(string viewName, TModel model, Controller? controller = null);
}
