using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swap.Htmx.Diagnostics;
using Swap.Htmx.Events;
using Swap.Htmx.Models;

// Import the SwapController from the root namespace
using SwapController = Swap.Htmx.SwapController;

namespace Swap.Htmx.ServerSentEvents;

/// <summary>
/// Interface for bridging domain events to SSE broadcasts.
/// </summary>
public interface ISseEventBridge
{
    /// <summary>
    /// Handles an SSE-related event and broadcasts it to appropriate connections.
    /// </summary>
    Task HandleSseEventAsync(string eventName, object? payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// Bridges domain events to SSE broadcasts by listening to event bus emissions.
/// </summary>
internal sealed class SseEventBridge : ISseEventBridge
{
    private readonly ISseConnectionRegistry _connectionRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly SwapEventBusOptions _eventBusOptions;
    private readonly ILogger<SseEventBridge> _logger;
    private readonly ISseViewRenderer _viewRenderer;

    public SseEventBridge(
        ISseConnectionRegistry connectionRegistry,
        IServiceProvider serviceProvider,
        SwapEventBusOptions eventBusOptions,
        ILogger<SseEventBridge> logger,
        ISseViewRenderer viewRenderer)
    {
        _connectionRegistry = connectionRegistry;
        _serviceProvider = serviceProvider;
        _eventBusOptions = eventBusOptions;
        _logger = logger;
        _viewRenderer = viewRenderer;
    }

    public async Task HandleSseEventAsync(string eventName, object? payload, CancellationToken cancellationToken = default)
    {
        using var activity = SwapTelemetry.ActivitySource.StartActivity("Swap.Htmx.SseBroadcast");
        activity?.SetTag("sse.event_key", eventName);
        
        SwapTelemetry.SseBroadcasts.Add(1);

        try
        {
            var (eventType, target, sseEventName) = ParseSseEvent(eventName);
            
            activity?.SetTag("sse.type", eventType);
            activity?.SetTag("sse.target", target);
            activity?.SetTag("sse.name", sseEventName);

            _logger.SseBroadcast(sseEventName, eventType, target ?? "all");

            string html = await RenderEventContentAsync(sseEventName, payload, cancellationToken);
            
            switch (eventType)
            {
                case "broadcast":
                    await _connectionRegistry.BroadcastAsync(sseEventName, html, cancellationToken);
                    break;

                case "room":
                    if (string.IsNullOrEmpty(target))
                    {
                        _logger.LogWarning("[SSE Bridge] Room target is missing for event {EventName}", eventName);
                        break;
                    }
                    await _connectionRegistry.BroadcastToRoomsAsync(sseEventName, html, new[] { target }, cancellationToken);
                    break;

                case "rooms":
                    if (string.IsNullOrEmpty(target))
                    {
                        _logger.LogWarning("[SSE Bridge] Rooms target is missing for event {EventName}", eventName);
                        break;
                    }
                    var rooms = target.Split(',');
                    await _connectionRegistry.BroadcastToRoomsAsync(sseEventName, html, rooms, cancellationToken);
                    break;

                case "subscribers":
                    await _connectionRegistry.BroadcastToSubscribersAsync(sseEventName, html, cancellationToken);
                    break;

                case "roles":
                    if (string.IsNullOrEmpty(target))
                    {
                        _logger.LogWarning("[SSE Bridge] Roles target is missing for event {EventName}", eventName);
                        break;
                    }
                    var roles = target.Split(',');
                    await _connectionRegistry.BroadcastToRolesAsync(sseEventName, html, roles, cancellationToken);
                    break;

                case "user":
                    if (string.IsNullOrEmpty(target))
                    {
                        _logger.LogWarning("[SSE Bridge] User target is missing for event {EventName}", eventName);
                        break;
                    }
                    await _connectionRegistry.BroadcastToUserAsync(sseEventName, html, target, cancellationToken);
                    break;

                case "filter":
                    if (string.IsNullOrEmpty(target))
                    {
                        _logger.LogWarning("[SSE Bridge] Filter target is missing for event {EventName}", eventName);
                        break;
                    }
                    await HandleFilteredBroadcast(target, sseEventName, html, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("[SSE Bridge] Unknown SSE event type: {EventType}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "[SSE Bridge] Error handling SSE event {EventName}", eventName);
            throw;
        }
    }


    /// <summary>
    /// Parses an SSE event name to extract type, target, and event name.
    /// Format: sse:{type}:{target}:{eventName} or sse:{type}:{eventName}
    /// </summary>
    private static (string eventType, string target, string sseEventName) ParseSseEvent(string eventName)
    {
        const string prefix = "sse:";
        if (!eventName.StartsWith(prefix))
        {
            throw new ArgumentException($"SSE event name must start with '{prefix}': {eventName}");
        }

        var parts = eventName[prefix.Length..].Split(':');

        return parts.Length switch
        {
            2 => (parts[0], string.Empty, parts[1]),  // sse:broadcast:eventName
            3 => (parts[0], parts[1], parts[2]),       // sse:room:roomName:eventName
            _ => throw new ArgumentException($"Invalid SSE event format: {eventName}")
        };
    }

    /// <summary>
    /// Renders HTML content for an SSE event by executing the configured event chain.
    /// </summary>
    private async Task<string> RenderEventContentAsync(string eventName, object? payload, CancellationToken cancellationToken)
    {
        using var activity = SwapTelemetry.ActivitySource.StartActivity("Swap.Htmx.SseRender");
        activity?.SetTag("sse.event_name", eventName);

        try
        {
            // Get the event chain configuration for this SSE event
            var configs = _eventBusOptions.GetEventChainConfigs();
            var fullEventName = $"sse:broadcast:{eventName}"; // Reconstruct the full event name
            
            if (!configs.TryGetValue(fullEventName, out var config))
            {
                _logger.SseRenderNoConfig(fullEventName);
                return $"<div data-event=\"{eventName}\"></div>";
            }

            _logger.SseRenderStart(eventName);

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

                    _logger.RenderingPartial(partial.ViewName, eventName);

                    // Use the standalone view renderer (doesn't need SwapController)
                    var partialHtml = await _viewRenderer.RenderPartialAsync(partial.ViewName, model);
                    
                    // For SSE broadcasts, HTMX SSE extension expects just the HTML content
                    // The element with sse-swap attribute will handle the swap
                    // We don't need hx-swap-oob wrapper for SSE events
                    htmlBuilder.AppendLine(partialHtml);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SSE Bridge] Failed to render partial {ViewName} for SSE event {EventName}", 
                        partial.ViewName, eventName);
                }
            }

            var result = htmlBuilder.ToString();
            _logger.SseRenderSuccess(eventName, result.Length);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.SseRenderError(ex, eventName);
            return $"<div data-event=\"{eventName}\"></div>";
        }
    }

    /// <summary>
    /// Attempts to render a partial view for the SSE event using the current controller context.
    /// </summary>
    private async Task<string?> TryRenderPartialViewAsync(string eventName, object? payload)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var httpContextAccessor = scope.ServiceProvider.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();

            if (httpContextAccessor?.HttpContext is null)
            {
                return null;
            }

            // Check if we're in a SwapController context
            if (httpContextAccessor.HttpContext.Items["SwapController"] is not SwapController controller)
            {
                return null;
            }

            // Try common partial view naming patterns
            var viewNames = new[]
            {
                $"_Sse{eventName}",
                $"_Sse_{eventName}",
                $"_{eventName}",
                eventName
            };

            foreach (var viewName in viewNames)
            {
                try
                {
                    return await controller.RenderPartialToStringAsync(viewName, payload);
                }
                catch
                {
                    // Continue to next view name
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to render partial view for SSE event: {EventName}", eventName);
        }

        return null;
    }

    /// <summary>
    /// Handles filtered broadcasting based on predefined filter keys.
    /// </summary>
    private async Task HandleFilteredBroadcast(string filterKey, string eventName, string html, CancellationToken cancellationToken)
    {
        Func<SseConnection, bool>? filter = filterKey.ToLowerInvariant() switch
        {
            "authenticated" => conn => conn.User?.Identity?.IsAuthenticated == true,
            "anonymous" => conn => conn.User?.Identity?.IsAuthenticated != true,
            "admin" => conn => conn.User?.IsInRole("admin") == true,
            "moderator" => conn => conn.User?.IsInRole("moderator") == true || conn.User?.IsInRole("admin") == true,
            "recent" => conn => (DateTime.UtcNow - conn.ConnectedAt).TotalMinutes < 5,
            "monitoring" => conn => conn.Rooms.Contains("monitoring"),
            "debug" => conn => conn.Rooms.Contains("debug"),
            _ => null
        };

        if (filter != null)
        {
            await _connectionRegistry.BroadcastToFilteredAsync(eventName, html, filter, cancellationToken);
        }
        else
        {
            _logger.SseUnknownFilter(filterKey);
        }
    }
}