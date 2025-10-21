using System.Threading;
using System.Threading.Tasks;

namespace NetMX.Events;

/// <summary>
/// Handles a specific event type.
/// Implement this interface to react to events published via IEventBus.
/// </summary>
/// <typeparam name="TData">Type of the event data.</typeparam>
public interface IEventHandler<in TData>
{
    /// <summary>
    /// Handles the event.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="data">The event data.</param>
    /// <param name="context">The event context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task HandleAsync(
        string eventName,
        TData data,
        EventContext context,
        CancellationToken cancellationToken = default);
}
