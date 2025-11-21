namespace Swap.Htmx.Realtime;

/// <summary>
/// Handles incoming messages from realtime connections (e.g. WebSockets).
/// </summary>
public interface IRealtimeInputHandler
{
    /// <summary>
    /// Processes a message received from a client.
    /// </summary>
    Task HandleMessageAsync(IRealtimeConnection connection, string message);
}
