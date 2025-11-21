namespace Swap.Htmx.Realtime;

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
