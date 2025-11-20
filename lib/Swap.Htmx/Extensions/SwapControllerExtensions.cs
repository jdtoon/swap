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
}
