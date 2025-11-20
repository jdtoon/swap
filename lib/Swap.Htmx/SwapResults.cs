using Swap.Htmx.Models;
using Swap.Htmx.Events;
using Swap.Htmx.Results;
using Swap.Htmx.Realtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace Swap.Htmx;

/// <summary>
/// Static entry point for Minimal APIs.
/// </summary>
public static class SwapResults
{
    /// <summary>
    /// Creates a fluent response builder for coordinating multiple updates in a single response.
    /// </summary>
    /// <returns>A fluent builder for constructing coordinated HTMX responses.</returns>
    public static SwapResponseBuilder Response()
    {
        return new SwapResponseBuilder();
    }

    /// <summary>
    /// Creates a response based on a configured event chain.
    /// </summary>
    /// <param name="eventKey">The event key to trigger.</param>
    /// <param name="payload">Optional payload for the event.</param>
    /// <returns>An IResult that executes the configured event chain.</returns>
    public static IResult Event(EventKey eventKey, object? payload = null)
    {
        return new SwapEventResult(eventKey, payload);
    }

    /// <summary>
    /// Creates a Server-Sent Events (SSE) connection result.
    /// </summary>
    /// <param name="registry">The SSE connection registry.</param>
    /// <param name="configure">Optional configuration for the SSE connection.</param>
    /// <returns>An IResult that establishes an SSE connection.</returns>
    public static IResult Sse(ISseConnectionRegistry registry, Action<SwapSseOptions>? configure = null)
    {
        return new SwapSseResult(registry, configure);
    }
}

/// <summary>
/// IResult implementation for executing event chains in Minimal APIs.
/// </summary>
internal class SwapEventResult : IResult
{
    private readonly EventKey _eventKey;
    private readonly object? _payload;

    public SwapEventResult(EventKey eventKey, object? payload)
    {
        _eventKey = eventKey;
        _payload = payload;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        var eventService = httpContext.RequestServices.GetRequiredService<ISwapEventService>();
        var builder = eventService.Event(_eventKey, _payload);
        return new SwapResult(builder).ExecuteAsync(httpContext);
    }
}
