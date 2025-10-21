using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NetMX.Events;

/// <summary>
/// In-memory event bus implementation using IMemoryCache (zero external dependencies).
/// Provides event publishing, deduplication, loop prevention, and rate limiting.
/// </summary>
public class EventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EventBus> _logger;
    
    private static readonly ActivitySource ActivitySource = new("NetMX.Events");
    
    // Per-request triggered events (for HTMX HX-Trigger headers)
    private static readonly ConcurrentDictionary<Guid, Dictionary<string, object>> 
        _triggeredEvents = new();
    
    // Event direction metadata (cached via reflection)
    private static readonly Dictionary<string, EventDirection> 
        _eventDirections = new();

    /// <summary>
    /// Creates a new EventBus instance.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving handlers.</param>
    /// <param name="cache">Memory cache for rate limiting and deduplication.</param>
    /// <param name="logger">Logger instance.</param>
    public EventBus(
        IServiceProvider serviceProvider,
        IMemoryCache cache,
        ILogger<EventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAsync<TData>(
        string eventName,
        TData data,
        EventContext? context = null,
        CancellationToken cancellationToken = default)
    {
        // Create context if not provided
        context ??= new EventContext();

        using var activity = ActivitySource.StartActivity("EventBus.Publish");
        activity?.SetTag("event.name", eventName);
        activity?.SetTag("event.depth", context.Depth);
        activity?.SetTag("request.id", context.RequestId);
        activity?.SetTag("session.id", context.SessionId);

        var startTime = Stopwatch.GetTimestamp();

        try
        {
            // 1. Check depth (circuit breaker)
            if (context.Depth >= EventContext.MaxDepth)
            {
                _logger.LogWarning(
                    "Event depth exceeded {MaxDepth} for event {EventName}. " +
                    "Origin: {Origin}. Stopping propagation.",
                    EventContext.MaxDepth, eventName, context.OriginEvent);
                activity?.SetTag("result", "depth_exceeded");
                return;
            }

            // 2. Check event budget
            if (context.EventCount >= EventContext.MaxEvents)
            {
                _logger.LogWarning(
                    "Event budget exceeded {MaxEvents} for request {RequestId}. " +
                    "Stopping event {EventName}.",
                    EventContext.MaxEvents, context.RequestId, eventName);
                activity?.SetTag("result", "budget_exceeded");
                return;
            }

            // 3. Create fingerprint (for deduplication)
            var fingerprint = CreateFingerprint(eventName, data, context);
            activity?.SetTag("event.fingerprint", fingerprint);

            // 4. Check if already processed (deduplication)
            if (context.ProcessedEvents.Contains(fingerprint))
            {
                _logger.LogDebug(
                    "Event {EventName} already processed in request {RequestId}. Skipping.",
                    eventName, context.RequestId);
                activity?.SetTag("result", "deduplicated");
                activity?.SetTag("cache.hit", true);
                return;
            }

            // 5. Check rate limiting (per-session, sliding window)
            if (await IsRateLimitedAsync(eventName, context, cancellationToken))
            {
                _logger.LogWarning(
                    "Rate limit exceeded for event {EventName} in session {SessionId}.",
                    eventName, context.SessionId);
                activity?.SetTag("result", "rate_limited");
                return;
            }

            // 6. Validate event direction (DAG enforcement)
            if (!ValidateEventDirection(eventName, context, activity))
            {
                activity?.SetTag("result", "direction_violated");
                return;
            }

            // 7. Mark as processed
            context.ProcessedEvents.Add(fingerprint);
            context.IncrementEventCount();

            // 8. Store triggered event (for HTMX headers)
            AddTriggeredEvent(context.RequestId, eventName, data);

            // 9. Find and execute handlers
            var handlers = GetHandlers<TData>();
            activity?.SetTag("handlers.count", handlers.Count);

            if (handlers.Count == 0)
            {
                _logger.LogDebug("No handlers registered for event {EventName}.", eventName);
                activity?.SetTag("result", "no_handlers");
                return;
            }

            // 10. Execute handlers
            foreach (var handler in handlers)
            {
                using var handlerActivity = ActivitySource.StartActivity("EventHandler.Execute");
                handlerActivity?.SetTag("handler.type", handler.GetType().Name);
                handlerActivity?.SetTag("event.name", eventName);

                try
                {
                    await handler.HandleAsync(eventName, data, context, cancellationToken);
                    handlerActivity?.SetTag("result", "success");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Handler {HandlerType} failed for event {EventName}.",
                        handler.GetType().Name, eventName);
                    handlerActivity?.SetTag("result", "error");
                    handlerActivity?.SetTag("error.message", ex.Message);
                    // Continue to other handlers
                }
            }

            var elapsed = Stopwatch.GetElapsedTime(startTime);
            activity?.SetTag("duration.ms", elapsed.TotalMilliseconds);
            activity?.SetTag("result", "success");

            _logger.LogDebug(
                "Published event {EventName} with {HandlerCount} handlers in {Duration}ms.",
                eventName, handlers.Count, elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventName}.", eventName);
            activity?.SetTag("result", "error");
            activity?.SetTag("error.message", ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public Dictionary<string, object> GetTriggeredEvents(Guid requestId)
    {
        if (_triggeredEvents.TryGetValue(requestId, out var events))
        {
            // Remove from tracking (request complete)
            _triggeredEvents.TryRemove(requestId, out _);
            return events;
        }

        return new Dictionary<string, object>();
    }

    /// <summary>
    /// Creates a fingerprint for event deduplication.
    /// Uses event name + serialized data + depth.
    /// </summary>
    private string CreateFingerprint<TData>(string eventName, TData data, EventContext context)
    {
        var json = JsonSerializer.Serialize(data);
        var input = $"{eventName}|{json}|{context.Depth}";
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Checks if event is rate limited (per-session, sliding window).
    /// Limit: 10 events per minute per session.
    /// </summary>
    private async Task<bool> IsRateLimitedAsync(
        string eventName,
        EventContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(context.SessionId))
            return false; // No session, no rate limiting

        var cacheKey = $"ratelimit:{context.SessionId}:{eventName}";
        var count = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return 0;
        });

        if (count >= 10)
            return true; // Rate limited

        _cache.Set(cacheKey, count + 1, TimeSpan.FromMinutes(1));
        return false;
    }

    /// <summary>
    /// Validates event direction to enforce DAG and prevent loops.
    /// </summary>
    private bool ValidateEventDirection(
        string eventName,
        EventContext context,
        Activity? activity)
    {
        var direction = GetEventDirection(eventName);
        activity?.SetTag("event.direction", direction.ToString());

        // Terminal events cannot trigger anything
        if (context.OriginEvent != null)
        {
            var originDirection = GetEventDirection(context.OriginEvent);
            if (originDirection == EventDirection.Terminal)
            {
                _logger.LogWarning(
                    "Terminal event {OriginEvent} attempted to trigger {EventName}. Blocked.",
                    context.OriginEvent, eventName);
                return false;
            }
        }

        // Downstream events cannot trigger Upstream events
        if (context.OriginEvent != null)
        {
            var originDirection = GetEventDirection(context.OriginEvent);
            if (originDirection == EventDirection.Downstream &&
                direction == EventDirection.Upstream)
            {
                _logger.LogWarning(
                    "Downstream event {OriginEvent} attempted to trigger Upstream event {EventName}. Blocked.",
                    context.OriginEvent, eventName);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets event direction from attribute (cached).
    /// Defaults to Downstream if not specified.
    /// </summary>
    private EventDirection GetEventDirection(string eventName)
    {
        if (_eventDirections.TryGetValue(eventName, out var direction))
            return direction;

        // Search all types for event constants with EventDirectionAttribute
        // This is cached, so reflection only happens once
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (field.FieldType == typeof(string) &&
                        field.GetValue(null) is string value &&
                        value == eventName)
                    {
                        var attr = field.GetCustomAttribute<EventDirectionAttribute>();
                        if (attr != null)
                        {
                            _eventDirections[eventName] = attr.Direction;
                            return attr.Direction;
                        }
                    }
                }
            }
        }

        // Default to Downstream if not found
        _eventDirections[eventName] = EventDirection.Downstream;
        return EventDirection.Downstream;
    }

    /// <summary>
    /// Adds triggered event to request tracking (for HTMX headers).
    /// </summary>
    private void AddTriggeredEvent<TData>(Guid requestId, string eventName, TData data)
    {
        var events = _triggeredEvents.GetOrAdd(requestId, _ => new Dictionary<string, object>());
        events[eventName] = data ?? new object();
    }

    /// <summary>
    /// Gets all registered handlers for the event data type.
    /// Uses DI to resolve handlers.
    /// </summary>
    private List<IEventHandler<TData>> GetHandlers<TData>()
    {
        using var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider
            .GetServices<IEventHandler<TData>>()
            .ToList();
    }
}
