using Swap.Htmx.Models;

namespace Swap.Htmx.Events;

/// <summary>
/// Defines a distributed UI event handler for processing events of type T.
/// Handlers are automatically discovered via assembly scanning and executed when events are triggered.
/// </summary>
/// <typeparam name="T">The event payload type.</typeparam>
public interface ISwapEventHandler<T>
{
    /// <summary>
    /// Handles the event by modifying the provided response builder to update the UI.
    /// </summary>
    /// <param name="event">The event payload.</param>
    /// <param name="builder">The response builder to modify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleAsync(T @event, SwapResponseBuilder builder, CancellationToken cancellationToken = default);
}