using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swap.Htmx.Events;

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
    private readonly ILogger<SseEventBridge> _logger;

    public SseEventBridge(
        ISseConnectionRegistry connectionRegistry,
        IServiceProvider serviceProvider,
        ILogger<SseEventBridge> logger)
    {
        _connectionRegistry = connectionRegistry;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task HandleSseEventAsync(string eventName, object? payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var (eventType, target, sseEventName) = ParseSseEvent(eventName);

            string html = await RenderEventContentAsync(sseEventName, payload, cancellationToken);

            switch (eventType)
            {
                case "broadcast":
                    await _connectionRegistry.BroadcastAsync(sseEventName, html, cancellationToken);
                    break;

                case "room":
                    await _connectionRegistry.BroadcastToRoomsAsync(sseEventName, html, new[] { target }, cancellationToken);
                    break;

                case "rooms":
                    var rooms = target.Split(',');
                    await _connectionRegistry.BroadcastToRoomsAsync(sseEventName, html, rooms, cancellationToken);
                    break;

                case "subscribers":
                    await _connectionRegistry.BroadcastToSubscribersAsync(sseEventName, html, cancellationToken);
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
    /// Renders HTML content for an SSE event.
    /// First tries to render a partial view, then falls back to JSON payload.
    /// </summary>
    private async Task<string> RenderEventContentAsync(string eventName, object? payload, CancellationToken cancellationToken)
    {
        // Try to render a partial view for the event
        if (await TryRenderPartialViewAsync(eventName, payload) is { } html)
        {
            return html;
        }

        // Fall back to simple HTML with JSON payload
        if (payload is not null)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            return $"<div data-event=\"{eventName}\" data-payload=\"{System.Web.HttpUtility.HtmlEncode(json)}\"></div>";
        }

        // Simple notification div
        return $"<div data-event=\"{eventName}\"></div>";
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
}