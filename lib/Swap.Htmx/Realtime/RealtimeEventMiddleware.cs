using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swap.Htmx.Events;

namespace Swap.Htmx.Realtime;

/// <summary>
/// Middleware that inspects resolved Swap events and forwards any
/// <c>"sse:"</c> or <c>"realtime:"</c>-prefixed events to the configured <see cref="IRealtimeEventBridge"/>.
/// </summary>
public sealed class RealtimeEventMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RealtimeEventMiddleware> _logger;

    public RealtimeEventMiddleware(RequestDelegate next, ILogger<RealtimeEventMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(async state =>
        {
            var httpContext = (HttpContext)state!;

            var eventBridge = httpContext.RequestServices.GetService<IRealtimeEventBridge>();
            if (eventBridge == null)
            {
                return;
            }

            var eventBus = httpContext.RequestServices.GetService<ISwapEventBus>();
            if (eventBus is SwapEventBus bus)
            {
                var resolved = bus.ResolveChains(httpContext);
                
                var realtimeEvents = resolved.Where(kv => 
                    kv.Key.StartsWith("sse:") || kv.Key.StartsWith("realtime:")).ToList();

                foreach (var evt in realtimeEvents)
                {
                    try
                    {
                        await eventBridge.HandleRealtimeEventAsync(evt.Key, evt.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error handling realtime event: {EventName}", evt.Key);
                    }
                }
            }
        }, context);

        await _next(context);
    }
}
