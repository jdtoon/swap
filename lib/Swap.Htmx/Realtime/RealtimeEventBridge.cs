using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swap.Htmx.Diagnostics;
using Swap.Htmx.Events;
using Swap.Htmx.Models;

namespace Swap.Htmx.Realtime;

/// <summary>
/// Interface for bridging domain events to realtime broadcasts.
/// </summary>
public interface IRealtimeEventBridge
{
    /// <summary>
    /// Handles a realtime-related event and broadcasts it to appropriate connections.
    /// </summary>
    Task HandleRealtimeEventAsync(string eventName, object? payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// Bridges domain events to realtime broadcasts by listening to event bus emissions.
/// </summary>
internal sealed class RealtimeEventBridge : IRealtimeEventBridge, ISseEventBridge
{
    private readonly IRealtimeConnectionRegistry _connectionRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly SwapEventBusOptions _eventBusOptions;
    private readonly ILogger<RealtimeEventBridge> _logger;
    private readonly ISseViewRenderer _viewRenderer;

    public RealtimeEventBridge(
        IRealtimeConnectionRegistry connectionRegistry,
        IServiceProvider serviceProvider,
        SwapEventBusOptions eventBusOptions,
        ILogger<RealtimeEventBridge> logger,
        ISseViewRenderer viewRenderer)
    {
        _connectionRegistry = connectionRegistry;
        _serviceProvider = serviceProvider;
        _eventBusOptions = eventBusOptions;
        _logger = logger;
        _viewRenderer = viewRenderer;
    }

    public async Task HandleRealtimeEventAsync(string eventName, object? payload, CancellationToken cancellationToken = default)
    {
        using var activity = SwapTelemetry.ActivitySource.StartActivity("Swap.Htmx.RealtimeBroadcast");
        activity?.SetTag("realtime.event_key", eventName);
        
        // SwapTelemetry.SseBroadcasts.Add(1); // Reuse SSE telemetry for now or add new

        try
        {
            var (eventType, target, realtimeEventName) = ParseRealtimeEvent(eventName);
            
            activity?.SetTag("realtime.type", eventType);
            activity?.SetTag("realtime.target", target);
            activity?.SetTag("realtime.name", realtimeEventName);

            _logger.LogInformation("Realtime Broadcast: {EventName} Type: {EventType} Target: {Target}", realtimeEventName, eventType, target ?? "all");

            string html = await RenderEventContentAsync(realtimeEventName, payload, cancellationToken);
            
            switch (eventType)
            {
                case "broadcast":
                    await _connectionRegistry.BroadcastAsync(realtimeEventName, html, cancellationToken);
                    break;

                case "room":
                    if (string.IsNullOrEmpty(target))
                    {
                        _logger.LogWarning("[Realtime Bridge] Room target is missing for event {EventName}", eventName);
                        break;
                    }
                    await _connectionRegistry.BroadcastToRoomsAsync(realtimeEventName, html, new[] { target }, cancellationToken);
                    break;

                case "rooms":
                    if (string.IsNullOrEmpty(target))
                    {
                        _logger.LogWarning("[Realtime Bridge] Rooms target is missing for event {EventName}", eventName);
                        break;
                    }
                    var rooms = target.Split(',');
                    await _connectionRegistry.BroadcastToRoomsAsync(realtimeEventName, html, rooms, cancellationToken);
                    break;

                case "subscribers":
                    await _connectionRegistry.BroadcastToSubscribersAsync(realtimeEventName, html, cancellationToken);
                    break;

                case "roles":
                    if (string.IsNullOrEmpty(target))
                    {
                        _logger.LogWarning("[Realtime Bridge] Roles target is missing for event {EventName}", eventName);
                        break;
                    }
                    var roles = target.Split(',');
                    await _connectionRegistry.BroadcastToRolesAsync(realtimeEventName, html, roles, cancellationToken);
                    break;

                case "user":
                    if (string.IsNullOrEmpty(target))
                    {
                        _logger.LogWarning("[Realtime Bridge] User target is missing for event {EventName}", eventName);
                        break;
                    }
                    await _connectionRegistry.BroadcastToUserAsync(realtimeEventName, html, target, cancellationToken);
                    break;

                case "filter":
                    if (string.IsNullOrEmpty(target))
                    {
                        _logger.LogWarning("[Realtime Bridge] Filter target is missing for event {EventName}", eventName);
                        break;
                    }
                    await HandleFilteredBroadcast(target, realtimeEventName, html, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("[Realtime Bridge] Unknown realtime event type: {EventType}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "[Realtime Bridge] Error handling realtime event {EventName}", eventName);
            throw;
        }
    }

    public Task HandleSseEventAsync(string eventName, object? payload, CancellationToken cancellationToken = default)
    {
        return HandleRealtimeEventAsync(eventName, payload, cancellationToken);
    }

    private static (string eventType, string target, string realtimeEventName) ParseRealtimeEvent(string eventName)
    {
        string prefix;
        if (eventName.StartsWith("sse:"))
        {
            prefix = "sse:";
        }
        else if (eventName.StartsWith("realtime:"))
        {
            prefix = "realtime:";
        }
        else
        {
            throw new ArgumentException($"Realtime event name must start with 'sse:' or 'realtime:': {eventName}");
        }

        var parts = eventName[prefix.Length..].Split(':');

        return parts.Length switch
        {
            2 => (parts[0], string.Empty, parts[1]),  // realtime:broadcast:eventName
            3 => (parts[0], parts[1], parts[2]),       // realtime:room:roomName:eventName
            _ => throw new ArgumentException($"Invalid realtime event format: {eventName}")
        };
    }

    private async Task<string> RenderEventContentAsync(string eventName, object? payload, CancellationToken cancellationToken)
    {
        using var activity = SwapTelemetry.ActivitySource.StartActivity("Swap.Htmx.RealtimeRender");
        activity?.SetTag("realtime.event_name", eventName);

        try
        {
            // Get the event chain configuration for this event
            // We check for "sse:broadcast:{eventName}" for backward compatibility and consistency
            var configs = _eventBusOptions.GetEventChainConfigs();
            var fullEventName = $"sse:broadcast:{eventName}"; 
            
            if (!configs.TryGetValue(fullEventName, out var config))
            {
                // Try "realtime:broadcast:{eventName}" if needed in future, but for now stick to sse prefix for config
                 _logger.LogDebug("No configuration found for event {EventName}", fullEventName);
                return $"<div data-event=\"{eventName}\"></div>";
            }

            // Create a scoped service provider for rendering
            using var scope = _serviceProvider.CreateScope();
            var httpContextAccessor = scope.ServiceProvider.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var httpContext = httpContextAccessor?.HttpContext;

            // Render all configured partials using the view renderer
            var htmlBuilder = new System.Text.StringBuilder();
            foreach (var partial in config.Partials)
            {
                try
                {
                    object? model = null;
                    if (httpContext != null)
                    {
                        // Use payload-aware factory if available
                        model = partial.ModelFactoryWithPayload != null
                            ? partial.ModelFactoryWithPayload(httpContext, payload)
                            : partial.ModelFactory?.Invoke(httpContext);
                    }

                    // Use the standalone view renderer
                    var partialHtml = await _viewRenderer.RenderPartialAsync(partial.ViewName, model);
                    
                    htmlBuilder.AppendLine(partialHtml);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Realtime Bridge] Failed to render partial {ViewName} for event {EventName}", 
                        partial.ViewName, eventName);
                }
            }

            var result = htmlBuilder.ToString();
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error rendering content for event {EventName}", eventName);
            return $"<div data-event=\"{eventName}\"></div>";
        }
    }

    private async Task HandleFilteredBroadcast(string filterKey, string eventName, string html, CancellationToken cancellationToken)
    {
        Func<IRealtimeConnection, bool>? filter = filterKey.ToLowerInvariant() switch
        {
            "authenticated" => conn => conn.User?.Identity?.IsAuthenticated == true,
            "anonymous" => conn => conn.User?.Identity?.IsAuthenticated != true,
            "admin" => conn => conn.User?.IsInRole("admin") == true,
            "moderator" => conn => conn.User?.IsInRole("moderator") == true || conn.User?.IsInRole("admin") == true,
            "recent" => conn => (DateTime.UtcNow - conn.ConnectedAt).TotalMinutes < 5,
            "monitoring" => conn => conn.IsInRoom("monitoring"),
            "debug" => conn => conn.IsInRoom("debug"),
            _ => null
        };

        if (filter != null)
        {
            await _connectionRegistry.BroadcastToFilteredAsync(eventName, html, filter, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Unknown filter key: {FilterKey}", filterKey);
        }
    }
}
