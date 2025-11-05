using System.Collections.Concurrent;

namespace Swap.Htmx.ServerEvents;

/// <summary>
/// Abstraction for a transport that can publish and subscribe to server-side events across processes.
/// Implementations may use in-memory queues, message brokers, or other mechanisms.
/// </summary>
public interface IServerEventTransport
{
    /// <summary>
    /// Publishes an event payload to the given key (topic/queue name).
    /// </summary>
    Task PublishAsync(string eventKey, ReadOnlyMemory<byte> payload, IReadOnlyDictionary<string, string>? headers = null, CancellationToken ct = default);

    /// <summary>
    /// Subscribes to an event key and receives raw payload and headers for each message.
    /// Returns an IDisposable to unsubscribe.
    /// </summary>
    IDisposable Subscribe(string eventKey, Func<ReadOnlyMemory<byte>, IReadOnlyDictionary<string, string>, CancellationToken, Task> onMessage);
}
