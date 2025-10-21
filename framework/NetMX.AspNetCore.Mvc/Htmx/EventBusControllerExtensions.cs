using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetMX.AspNetCore.Events;
using NetMX.Events;

namespace NetMX.AspNetCore.Mvc.Htmx;

/// <summary>
/// Controller extensions for publishing events via EventBus.
/// Automatically integrates with EventBusMiddleware for HTMX header injection.
/// </summary>
public static class EventBusControllerExtensions
{
    /// <summary>
    /// Publishes an event via EventBus using the current request's EventContext.
    /// The event will be automatically added to HX-Trigger headers by EventBusMiddleware.
    /// </summary>
    /// <typeparam name="TData">Type of the event data.</typeparam>
    /// <param name="controller">The controller instance.</param>
    /// <param name="eventBus">The event bus instance (inject via DI).</param>
    /// <param name="eventName">The event name (use static constants from DomainEvents).</param>
    /// <param name="data">The event data payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <example>
    /// <code>
    /// [HttpPost]
    /// public async Task&lt;IActionResult&gt; Create(CreateProductDto dto)
    /// {
    ///     var product = await _service.CreateAsync(dto);
    ///     
    ///     // Publish event - will trigger HTMX listeners
    ///     await this.PublishEventAsync(_eventBus, 
    ///         DomainEvents.Product.Created, 
    ///         new { productId = product.Id });
    ///     
    ///     return Ok();
    /// }
    /// </code>
    /// </example>
    public static async Task PublishEventAsync<TData>(
        this ControllerBase controller,
        IEventBus eventBus,
        string eventName,
        TData data,
        CancellationToken cancellationToken = default)
    {
        var eventContext = controller.HttpContext.GetEventContext();
        await eventBus.PublishAsync(eventName, data, eventContext, cancellationToken);
    }

    /// <summary>
    /// Publishes an event and creates a child EventContext for cascading events.
    /// Use this when you want to trigger events from within an event handler.
    /// </summary>
    /// <typeparam name="TData">Type of the event data.</typeparam>
    /// <param name="controller">The controller instance.</param>
    /// <param name="eventBus">The event bus instance.</param>
    /// <param name="eventName">The event name.</param>
    /// <param name="data">The event data payload.</param>
    /// <param name="originEvent">The name of the originating event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public static async Task PublishCascadingEventAsync<TData>(
        this ControllerBase controller,
        IEventBus eventBus,
        string eventName,
        TData data,
        string originEvent,
        CancellationToken cancellationToken = default)
    {
        var parentContext = controller.HttpContext.GetEventContext();
        var childContext = parentContext.CreateChild(originEvent);
        await eventBus.PublishAsync(eventName, data, childContext, cancellationToken);
    }

    /// <summary>
    /// Gets the EventContext for the current HTTP request.
    /// </summary>
    /// <param name="controller">The controller instance.</param>
    /// <returns>The EventContext for this request.</returns>
    public static EventContext GetEventContext(this ControllerBase controller)
    {
        return controller.HttpContext.GetEventContext();
    }
}
