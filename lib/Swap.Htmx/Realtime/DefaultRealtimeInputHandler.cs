using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swap.Htmx.Events;
using Swap.Htmx.Services;

namespace Swap.Htmx.Realtime;

/// <summary>
/// Default implementation of IRealtimeInputHandler that triggers Swap events from JSON messages.
/// </summary>
public class DefaultRealtimeInputHandler : IRealtimeInputHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DefaultRealtimeInputHandler> _logger;

    public DefaultRealtimeInputHandler(IServiceProvider serviceProvider, ILogger<DefaultRealtimeInputHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task HandleMessageAsync(IRealtimeConnection connection, string message)
    {
        try
        {
            // Expected format: { "event": "eventName", "payload": { ... } }
            using var doc = JsonDocument.Parse(message);
            if (doc.RootElement.TryGetProperty("event", out var eventProp))
            {
                var eventName = eventProp.GetString();
                if (string.IsNullOrEmpty(eventName)) return;

                object? payload = null;
                if (doc.RootElement.TryGetProperty("payload", out var payloadProp))
                {
                    payload = payloadProp.Clone(); 
                }

                _logger.LogDebug("Received realtime event: {EventName}. Raw message: {Message}", eventName, message);

                var eventService = _serviceProvider.GetRequiredService<ISwapEventService>();
                var bus = _serviceProvider.GetRequiredService<ISwapEventBus>();
                var bridge = _serviceProvider.GetRequiredService<IRealtimeEventBridge>();
                var httpContextAccessor = _serviceProvider.GetRequiredService<IHttpContextAccessor>();
                
                // Trigger event (adds to bus)
                eventService.Event(new EventKey(eventName), payload);
                
                // Process bus
                if (bus is SwapEventBus swapBus && httpContextAccessor.HttpContext != null)
                {
                    var resolved = swapBus.ResolveChains(httpContextAccessor.HttpContext);
                    
                    _logger.LogDebug("Resolved {Count} events from chain", resolved.Count);
                    foreach(var k in resolved.Keys) _logger.LogDebug("Resolved event: {Key}", k);

                    var realtimeEvents = resolved.Where(kv => 
                        kv.Key.StartsWith("sse:") || kv.Key.StartsWith("realtime:")).ToList();

                    _logger.LogDebug("Found {Count} realtime events to broadcast", realtimeEvents.Count);

                    foreach (var evt in realtimeEvents)
                    {
                        await bridge.HandleRealtimeEventAsync(evt.Key, evt.Value);
                    }
                    
                    // Clear pending events to avoid re-processing in the same request scope
                    httpContextAccessor.HttpContext.Items.Remove(SwapEventKeys.PendingEvents);
                }
            }
        }
        catch (JsonException)
        {
            _logger.LogTrace("Received non-JSON message: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling WebSocket message");
        }
    }
}
