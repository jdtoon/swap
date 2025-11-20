using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Swap.Htmx.Events;
using Swap.Htmx.Models;

namespace Swap.Htmx;

/// <summary>
/// Extension methods for PageModel to enable Swap.Htmx features.
/// </summary>
public static class SwapPageModelExtensions
{
    /// <summary>
    /// Creates a fluent response builder for coordinating multiple updates in a single response.
    /// </summary>
    /// <param name="pageModel">The page model instance.</param>
    /// <returns>A fluent builder for constructing coordinated HTMX responses.</returns>
    public static SwapResponseBuilder SwapResponse(this PageModel pageModel)
    {
        var service = pageModel.HttpContext.RequestServices.GetRequiredService<ISwapEventService>();
        return service.Response(pageModel);
    }

    /// <summary>
    /// Executes a configured event chain and returns the coordinated response.
    /// </summary>
    /// <param name="pageModel">The page model instance.</param>
    /// <param name="eventKey">The event to trigger.</param>
    /// <param name="payload">Optional payload data to include with the event.</param>
    /// <returns>A SwapResponseBuilder with all configured partials, toasts, and triggers.</returns>
    public static SwapResponseBuilder SwapEvent(this PageModel pageModel, EventKey eventKey, object? payload = null)
    {
        var service = pageModel.HttpContext.RequestServices.GetRequiredService<ISwapEventService>();
        return service.Event(eventKey, pageModel, payload);
    }

    /// <summary>
    /// Asynchronously executes a configured event chain and returns the coordinated response.
    /// </summary>
    /// <param name="pageModel">The page model instance.</param>
    /// <param name="eventKey">The event to trigger.</param>
    /// <param name="payload">Optional payload data to include with the event.</param>
    /// <returns>A SwapResponseBuilder with all configured partials, toasts, and triggers.</returns>
    public static Task<SwapResponseBuilder> SwapEventAsync(this PageModel pageModel, EventKey eventKey, object? payload = null)
    {
        var service = pageModel.HttpContext.RequestServices.GetRequiredService<ISwapEventService>();
        return service.EventAsync(eventKey, pageModel, payload);
    }
}
