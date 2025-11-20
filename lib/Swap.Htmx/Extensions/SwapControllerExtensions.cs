using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Swap.Htmx.Events;
using Swap.Htmx.Models;

namespace Swap.Htmx;

/// <summary>
/// Extension methods for ControllerBase to enable Swap.Htmx features without inheriting from SwapController.
/// </summary>
public static class SwapControllerExtensions
{
    /// <summary>
    /// Creates a fluent response builder for coordinating multiple updates in a single response.
    /// </summary>
    /// <param name="controller">The controller instance.</param>
    /// <returns>A fluent builder for constructing coordinated HTMX responses.</returns>
    public static SwapResponseBuilder SwapResponse(this ControllerBase controller)
    {
        var service = controller.HttpContext.RequestServices.GetRequiredService<ISwapEventService>();
        return service.Response(controller);
    }

    /// <summary>
    /// Executes a configured event chain and returns the coordinated response.
    /// </summary>
    /// <param name="controller">The controller instance.</param>
    /// <param name="eventKey">The event to trigger.</param>
    /// <param name="payload">Optional payload data to include with the event.</param>
    /// <returns>A SwapResponseBuilder with all configured partials, toasts, and triggers.</returns>
    public static SwapResponseBuilder SwapEvent(this ControllerBase controller, EventKey eventKey, object? payload = null)
    {
        var service = controller.HttpContext.RequestServices.GetRequiredService<ISwapEventService>();
        return service.Event(eventKey, controller, payload);
    }

    /// <summary>
    /// Asynchronously executes a configured event chain and returns the coordinated response.
    /// </summary>
    /// <param name="controller">The controller instance.</param>
    /// <param name="eventKey">The event to trigger.</param>
    /// <param name="payload">Optional payload data to include with the event.</param>
    /// <returns>A SwapResponseBuilder with all configured partials, toasts, and triggers.</returns>
    public static Task<SwapResponseBuilder> SwapEventAsync(this ControllerBase controller, EventKey eventKey, object? payload = null)
    {
        var service = controller.HttpContext.RequestServices.GetRequiredService<ISwapEventService>();
        return service.EventAsync(eventKey, controller, payload);
    }

    /// <summary>
    /// Returns a view result that automatically chooses between full page or partial view
    /// based on whether the request is an HTMX request (HX-Request header present).
    /// </summary>
    /// <param name="controller">The controller instance.</param>
    /// <param name="viewName">The name of the view to render. If null, uses conventional view name.</param>
    /// <param name="model">The model to pass to the view.</param>
    /// <returns>
    /// - For HTMX requests (HX-Request header present): Returns partial view
    /// - For normal requests (initial page load, refresh): Returns full view with layout
    /// </returns>
    public static IActionResult SwapView(this Controller controller, string? viewName, object? model = null)
    {
        controller.Response.EnsureVaryHxRequest();

        if (controller.Request.IsHtmxRequest())
        {
            return controller.PartialView(viewName, model);
        }
        
        return controller.View(viewName, model);
    }

    /// <summary>
    /// Returns a view result that automatically chooses between full page or partial view
    /// based on whether the request is an HTMX request (HX-Request header present).
    /// </summary>
    /// <param name="controller">The controller instance.</param>
    /// <param name="model">The model to pass to the view.</param>
    /// <returns>
    /// - For HTMX requests (HX-Request header present): Returns partial view
    /// - For normal requests (initial page load, refresh): Returns full view with layout
    /// </returns>
    public static IActionResult SwapView(this Controller controller, object? model = null)
    {
        return controller.SwapView(viewName: null, model: model);
    }
}
