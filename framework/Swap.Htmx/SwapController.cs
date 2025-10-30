using Microsoft.AspNetCore.Mvc;

namespace Swap.Htmx;

/// <summary>
/// Base controller for HTMX-enabled applications.
/// Automatically handles page vs partial rendering based on HX-Request header.
/// </summary>
public abstract class SwapController : Controller
{
    /// <summary>
    /// Returns a view result that automatically chooses between full page or partial view
    /// based on whether the request is an HTMX request (HX-Request header present).
    /// </summary>
    /// <param name="viewName">The name of the view to render. If null, uses conventional view name.</param>
    /// <param name="model">The model to pass to the view.</param>
    /// <returns>
    /// - For HTMX requests (HX-Request header present): Returns partial view
    /// - For normal requests (initial page load, refresh): Returns full view with layout
    /// </returns>
    /// <example>
    /// <code>
    /// public async Task&lt;IActionResult&gt; Index()
    /// {
    ///     var articles = await _context.Articles.ToListAsync();
    ///     return SwapView(articles);
    /// }
    /// </code>
    /// </example>
    protected IActionResult SwapView(object? model = null)
    {
        return SwapView(viewName: null, model: model);
    }

    /// <summary>
    /// Returns a view result that automatically chooses between full page or partial view
    /// based on whether the request is an HTMX request (HX-Request header present).
    /// </summary>
    /// <param name="viewName">The name of the view to render. If null, uses conventional view name.</param>
    /// <param name="model">The model to pass to the view.</param>
    /// <returns>
    /// - For HTMX requests (HX-Request header present): Returns partial view
    /// - For normal requests (initial page load, refresh): Returns full view with layout
    /// </returns>
    /// <example>
    /// <code>
    /// public async Task&lt;IActionResult&gt; Details(int id)
    /// {
    ///     var article = await _context.Articles.FindAsync(id);
    ///     return SwapView("Details", article);
    /// }
    /// </code>
    /// </example>
    protected IActionResult SwapView(string? viewName, object? model = null)
    {
        bool isHtmxRequest = Request.Headers.ContainsKey("HX-Request");

        if (isHtmxRequest)
        {
            // HTMX request - return partial view without layout
            return PartialView(viewName, model);
        }
        else
        {
            // Normal request (initial load or refresh) - return full view with layout
            return View(viewName, model);
        }
    }
}
