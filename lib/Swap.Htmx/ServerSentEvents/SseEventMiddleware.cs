using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swap.Htmx.Events;

namespace Swap.Htmx.ServerSentEvents;

/// <summary>
/// Middleware that inspects resolved Swap events and forwards any
/// <c>"sse:"</c>-prefixed events to the configured <see cref="ISseEventBridge"/>.
/// This enables automatic event-driven SSE broadcasting from the same
/// event chains that feed <c>HX-Trigger</c>.
/// </summary>
public sealed class SseEventMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SseEventMiddleware> _logger;

    public SseEventMiddleware(RequestDelegate next, ILogger<SseEventMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Register callback to handle SSE events after the request completes
        context.Response.OnStarting(async state =>
        {
            var httpContext = (HttpContext)state!;

            // Check if SSE event bridge is available
            var eventBridge = httpContext.RequestServices.GetService<ISseEventBridge>();
            if (eventBridge == null)
            {
                return;
            }

            // Get pending events from the event bus
            var eventBus = httpContext.RequestServices.GetService<ISwapEventBus>();
            if (eventBus is SwapEventBus bus)
            {
                var resolved = bus.ResolveChains(httpContext);

                // Process SSE events
                var sseEvents = resolved.Where(kv => kv.Key.StartsWith("sse:"));

                foreach (var sseEvent in sseEvents)
                {
                    try
                    {
                        await eventBridge.HandleSseEventAsync(sseEvent.Key, sseEvent.Value);
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't break the response
                        _logger?.LogError(ex, "Error handling SSE event: {EventName}", sseEvent.Key);
                    }
                }
            }
        }, context);

        await _next(context);
    }
}