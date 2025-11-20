using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swap.Htmx.Events;
using Swap.Htmx.Models;

namespace Swap.Htmx;

/// <summary>
/// Service for handling Swap.Htmx operations without inheriting from SwapController.
/// Enables "Composition over Inheritance" for better flexibility.
/// </summary>
public interface ISwapEventService
{
    /// <summary>
    /// Creates a fluent response builder for coordinating multiple updates in a single response.
    /// </summary>
    /// <param name="controller">The controller instance (needed for View rendering context).</param>
    /// <returns>A fluent builder for constructing coordinated HTMX responses.</returns>
    SwapResponseBuilder Response(ControllerBase controller);

    /// <summary>
    /// Executes a configured event chain and returns the coordinated response.
    /// </summary>
    /// <param name="eventKey">The event to trigger.</param>
    /// <param name="controller">The controller instance.</param>
    /// <param name="payload">Optional payload data to include with the event.</param>
    /// <returns>A SwapResponseBuilder with all configured partials, toasts, and triggers.</returns>
    SwapResponseBuilder Event(EventKey eventKey, ControllerBase controller, object? payload = null);

    /// <summary>
    /// Asynchronously executes a configured event chain and returns the coordinated response.
    /// Use this when your event chain includes async model factories.
    /// </summary>
    /// <param name="eventKey">The event to trigger.</param>
    /// <param name="controller">The controller instance.</param>
    /// <param name="payload">Optional payload data to include with the event.</param>
    /// <returns>A SwapResponseBuilder with all configured partials, toasts, and triggers.</returns>
    Task<SwapResponseBuilder> EventAsync(EventKey eventKey, ControllerBase controller, object? payload = null);
}
