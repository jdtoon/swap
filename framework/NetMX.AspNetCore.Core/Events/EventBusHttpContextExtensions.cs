using System;
using Microsoft.AspNetCore.Http;
using NetMX.Events;

namespace NetMX.AspNetCore.Events;

/// <summary>
/// Extension methods for accessing EventContext from HttpContext.
/// </summary>
public static class EventBusHttpContextExtensions
{
    private const string EventContextKey = "NetMX.EventContext";

    /// <summary>
    /// Gets the EventContext for the current HTTP request.
    /// Returns a new EventContext if none exists (fallback for requests outside middleware).
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The EventContext for this request.</returns>
    public static EventContext GetEventContext(this HttpContext context)
    {
        if (context.Items.TryGetValue(EventContextKey, out var obj) && obj is EventContext eventContext)
        {
            return eventContext;
        }

        // Fallback: Create a new context if middleware didn't run
        // This can happen in tests or if middleware is not registered
        return new EventContext
        {
            RequestId = Guid.NewGuid(),
            SessionId = string.Empty,
            UserId = null,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Checks if an EventContext exists for the current HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>True if EventContext exists, false otherwise.</returns>
    public static bool HasEventContext(this HttpContext context)
    {
        return context.Items.ContainsKey(EventContextKey);
    }

    /// <summary>
    /// Sets the EventContext for the current HTTP request.
    /// Typically used by EventBusMiddleware, but can be used in tests.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="eventContext">The EventContext to store.</param>
    internal static void SetEventContext(this HttpContext context, EventContext eventContext)
    {
        context.Items[EventContextKey] = eventContext;
    }
}
