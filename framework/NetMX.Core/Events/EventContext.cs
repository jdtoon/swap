using System;
using System.Collections.Generic;

namespace NetMX.Events;

/// <summary>
/// Contains metadata and state for event processing within a single HTTP request.
/// Prevents infinite loops and duplicate event processing.
/// </summary>
public class EventContext
{
    /// <summary>
    /// Unique identifier for the HTTP request.
    /// All events triggered within the same request share this ID.
    /// </summary>
    public Guid RequestId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// User session identifier.
    /// Used for cross-request rate limiting and user-scoped deduplication.
    /// </summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>
    /// Authenticated user ID (if logged in).
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// When the request started (UTC).
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Current depth in the event chain (0 = initial event, 1 = triggered by initial, etc.).
    /// Used to prevent infinite loops via max depth enforcement.
    /// </summary>
    public int Depth { get; private set; } = 0;

    /// <summary>
    /// The event that triggered the current event (if any).
    /// Used for tracing and loop detection.
    /// </summary>
    public string? OriginEvent { get; private set; }

    /// <summary>
    /// Set of event fingerprints already processed in this request.
    /// Prevents duplicate event processing.
    /// </summary>
    public HashSet<string> ProcessedEvents { get; } = new();

    /// <summary>
    /// Total number of events processed in this request.
    /// Used for event budget enforcement.
    /// </summary>
    public int EventCount { get; private set; } = 0;

    /// <summary>
    /// Maximum allowed depth for event chains.
    /// Prevents infinite loops via circuit breaker.
    /// </summary>
    public const int MaxDepth = 10;

    /// <summary>
    /// Maximum allowed events per request.
    /// Prevents event bombs and resource exhaustion.
    /// </summary>
    public const int MaxEvents = 50;

    /// <summary>
    /// Creates a child context for a chained event.
    /// Increments depth and tracks origin for loop detection.
    /// </summary>
    /// <param name="triggeringEvent">The event that is triggering the new event.</param>
    /// <returns>A new EventContext with incremented depth.</returns>
    /// <exception cref="InvalidOperationException">Thrown when max depth or event budget exceeded.</exception>
    public EventContext CreateChild(string triggeringEvent)
    {
        if (Depth >= MaxDepth)
        {
            throw new InvalidOperationException(
                $"Event depth exceeded {MaxDepth} (possible infinite loop). " +
                $"Origin: {OriginEvent ?? "(root)"} → {triggeringEvent}");
        }

        if (EventCount >= MaxEvents)
        {
            throw new InvalidOperationException(
                $"Event budget exceeded {MaxEvents} events per request. " +
                $"This may indicate an event loop or misconfiguration.");
        }

        var child = new EventContext
        {
            RequestId = RequestId,              // Same request
            SessionId = SessionId,              // Same session
            UserId = UserId,                    // Same user
            Timestamp = Timestamp,              // Same request timestamp
            Depth = Depth + 1,                  // Increment depth
            OriginEvent = triggeringEvent,      // Track what triggered this
            EventCount = EventCount + 1         // Increment count
        };

        // Share the ProcessedEvents set (by reference) with the child
        foreach (var processed in ProcessedEvents)
        {
            child.ProcessedEvents.Add(processed);
        }

        return child;
    }

    /// <summary>
    /// Increments the event count without creating a child context.
    /// Used when processing multiple handlers for the same event.
    /// </summary>
    public void IncrementEventCount()
    {
        EventCount++;
    }
}
