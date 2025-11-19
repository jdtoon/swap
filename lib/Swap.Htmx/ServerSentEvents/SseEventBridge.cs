using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

    public SseEventBridge(
        ISseConnectionRegistry connectionRegistry,
        IServiceProvider serviceProvider,
        SwapEventBusOptions eventBusOptions,
        ILogger<SseEventBridge> logger)
    {
        _connectionRegistry = connectionRegistry;
        _serviceProvider = serviceProvider;
        _eventBusOptions = eventBusOptions;
        _logger = logger;
    }

    public async Task HandleSseEventAsync(string eventName, object? payload, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("[SSE Bridge] HandleSseEventAsync called with eventName: {EventName}, payload type: {PayloadType}", 
                eventName, payload?.GetType().Name ?? "null");
            
            var (eventType, target, sseEventName) = ParseSseEvent(eventName);
            _logger.LogDebug("[SSE Bridge] Parsed event - Type: {EventType}, Target: {Target}, SseEventName: {SseEventName}", 
                eventType, target ?? "null", sseEventName);

            string html = await RenderEventContentAsync(sseEventName, payload, cancellationToken);
            _logger.LogDebug("[SSE Bridge] Rendered HTML content (length: {Length} chars)", html.Length);

            switch (eventType)
            {
                case "broadcast":
                    _logger.LogDebug("[SSE Bridge] Broadcasting to all connections: {EventName}", sseEventName);
                    await _connectionRegistry.BroadcastAsync(sseEventName, html, cancellationToken);
                    break;

                case "room":
                    _logger.LogDebug("[SSE Bridge] Broadcasting to room: {Room}", target);
                    await _connectionRegistry.BroadcastToRoomsAsync(sseEventName, html, new[] { target }, cancellationToken);
                    break;

                case "rooms":
                    var rooms = target.Split(',');
                    _logger.LogDebug("[SSE Bridge] Broadcasting to rooms: {Rooms}", string.Join(", ", rooms));
                    await _connectionRegistry.BroadcastToRoomsAsync(sseEventName, html, rooms, cancellationToken);
                    break;

                case "subscribers":
                    _logger.LogDebug("[SSE Bridge] Broadcasting to subscribers of: {EventName}", sseEventName);
                    await _connectionRegistry.BroadcastToSubscribersAsync(sseEventName, html, cancellationToken);
                    break;

                case "roles":
                    var roles = target.Split(',');
                    _logger.LogDebug("[SSE Bridge] Broadcasting to roles: {Roles}", string.Join(", ", roles));
                    await _connectionRegistry.BroadcastToRolesAsync(sseEventName, html, roles, cancellationToken);
                    break;

                case "user":
                    _logger.LogDebug("[SSE Bridge] Broadcasting to user: {User}", target);
                    await _connectionRegistry.BroadcastToUserAsync(sseEventName, html, target, cancellationToken);
                    break;

                case "filter":
                    // For custom filters, we'll use a convention where the filter key maps to a predicate
                    _logger.LogDebug("[SSE Bridge] Broadcasting with filter: {Filter}", target);
                    await HandleFilteredBroadcast(target, sseEventName, html, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Unknown SSE event type: {EventType} for event {EventName}", eventType, eventName);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling SSE event: {EventName}", eventName);
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
        try
        {
            // Get the event chain configuration for this SSE event
            var configs = _eventBusOptions.GetEventChainConfigs();
            var fullEventName = $"sse:broadcast:{eventName}"; // Reconstruct the full event name
            
            if (!configs.TryGetValue(fullEventName, out var config))
            {
                _logger.LogDebug("[SSE Bridge] No event chain configuration found for {EventName}", fullEventName);
                return $"<div data-event=\"{eventName}\"></div>";
            }

            _logger.LogDebug("[SSE Bridge] Found event chain with {PartialCount} partials for {EventName}", 
                config.Partials.Count, eventName);

            // Create a minimal HTTP context if needed
            using var scope = _serviceProvider.CreateScope();
            var httpContextAccessor = scope.ServiceProvider.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var httpContext = httpContextAccessor?.HttpContext;

            if (httpContext == null)
            {
                _logger.LogWarning("[SSE Bridge] No HttpContext available for rendering SSE event: {EventName}", eventName);
                return $"<div data-event=\"{eventName}\"></div>";
            }

            // Check if we have a SwapController available
            if (httpContext.Items["SwapController"] is not SwapController controller)
            {
                _logger.LogWarning("[SSE Bridge] No SwapController available for rendering SSE event: {EventName}", eventName);
                return $"<div data-event=\"{eventName}\"></div>";
            }

            // Render all configured partials
            var htmlBuilder = new System.Text.StringBuilder();
            foreach (var partial in config.Partials)
            {
                try
                {
                    // Use payload-aware factory if available
                    var model = partial.ModelFactoryWithPayload != null
                        ? partial.ModelFactoryWithPayload(httpContext, payload)
                        : partial.ModelFactory?.Invoke(httpContext);

                    _logger.LogDebug("[SSE Bridge] Rendering partial {ViewName} with model type {ModelType}", 
                        partial.ViewName, model?.GetType().Name ?? "null");

                    var partialHtml = await controller.RenderPartialToStringAsync(partial.ViewName, model);
                    
                    // Wrap with hx-swap-oob for HTMX SSE
                    var swapMode = partial.SwapMode switch
                    {
                        SwapMode.OuterHTML => "true",
                        SwapMode.InnerHTML => "innerHTML",
                        SwapMode.BeforeBegin => "beforebegin",
                        SwapMode.AfterBegin => "afterbegin",
                        SwapMode.BeforeEnd => "beforeend",
                        SwapMode.AfterEnd => "afterend",
                        SwapMode.Delete => "delete",
                        _ => "true"
                    };

                    htmlBuilder.AppendLine($"<div id=\"{partial.TargetId}\" hx-swap-oob=\"{swapMode}\">");
                    htmlBuilder.AppendLine(partialHtml);
                    htmlBuilder.AppendLine("</div>");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SSE Bridge] Failed to render partial {ViewName} for SSE event {EventName}", 
                        partial.ViewName, eventName);
                }
            }

            var result = htmlBuilder.ToString();
            _logger.LogDebug("[SSE Bridge] Rendered {Length} chars of HTML for {EventName}", result.Length, eventName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SSE Bridge] Error rendering content for SSE event: {EventName}", eventName);
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
        Func<SseConnection, bool> filter = filterKey.ToLowerInvariant() switch
        {
            "authenticated" => conn => conn.User?.Identity?.IsAuthenticated == true,
            "anonymous" => conn => conn.User?.Identity?.IsAuthenticated != true,
            "admin" => conn => conn.User?.IsInRole("admin") == true,
            "moderator" => conn => conn.User?.IsInRole("moderator") == true || conn.User?.IsInRole("admin") == true,
            "recent" => conn => (DateTime.UtcNow - conn.ConnectedAt).TotalMinutes < 5,
            "monitoring" => conn => conn.Rooms.Contains("monitoring"),
            "debug" => conn => conn.Rooms.Contains("debug"),
            _ => _ => false // Unknown filter, matches nothing
        };

        if (filter != null)
        {
            await _connectionRegistry.BroadcastToFilteredAsync(eventName, html, filter, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Unknown SSE filter key: {FilterKey}", filterKey);
        }
    }
}