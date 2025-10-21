using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetMX.Events;

/// <summary>
/// Central event bus for publishing and handling events within the application.
/// Provides deduplication, loop prevention, rate limiting, and observability.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event with associated data.
    /// </summary>
    /// <typeparam name="TData">Type of the event data payload.</typeparam>
    /// <param name="eventName">Static event name (e.g., DomainEvents.Order.Created).</param>
    /// <param name="data">Event data payload.</param>
    /// <param name="context">Optional event context (auto-created if null).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task PublishAsync<TData>(
        string eventName,
        TData data,
        EventContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all events triggered during the specified request.
    /// Used by EventBusMiddleware to inject HX-Trigger headers.
    /// </summary>
    /// <param name="requestId">The request ID to get events for.</param>
    /// <returns>Dictionary of event names to event data (for HTMX headers).</returns>
    Dictionary<string, object> GetTriggeredEvents(Guid requestId);
}
