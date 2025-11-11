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
        // Content varies based on HX-Request; communicate this for caches/CDNs
        Response.EnsureVaryHxRequest();

        bool isHtmxRequest = Request.IsHtmxRequest();

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

    /// <summary>
    /// Returns an out-of-band (OOB) partial view with the hx-swap-oob attribute.
    /// Use this to update multiple parts of the page in a single response.
    /// </summary>
    /// <param name="targetId">The ID of the element to swap into (used in hx-swap-oob attribute).</param>
    /// <param name="viewName">The name of the view to render. If null, uses conventional view name.</param>
    /// <param name="model">The model to pass to the view.</param>
    /// <param name="swapStrategy">The swap strategy (defaults to "true" which means "outerHTML"). Can be "innerHTML", "beforebegin", "afterbegin", "beforeend", "afterend", "delete", "none".</param>
    /// <returns>A partial view with hx-swap-oob attribute for out-of-band swapping.</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;IActionResult&gt; UpdateCartAndTotal(int productId)
    /// {
    ///     // Update main content
    ///     var mainContent = SwapView("ProductAdded");
    ///     
    ///     // Also update cart total out-of-band
    ///     var cartTotal = await _cartService.GetTotalAsync();
    ///     ViewData["OobCartTotal"] = SwapOobView("cart-total", "_CartTotal", cartTotal);
    ///     
    ///     return mainContent;
    /// }
    /// </code>
    /// </example>
    protected IActionResult SwapOobView(string targetId, string? viewName = null, object? model = null, string swapStrategy = "true")
    {
        ViewData["HxSwapOob"] = swapStrategy;
        ViewData["OobTargetId"] = targetId;
        return PartialView(viewName, model);
    }
}
