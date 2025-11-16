using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Swap.Htmx.ServerEvents;

/// <summary>
/// Simple in-memory transport suitable for local development and tests.
/// Subscribers are invoked on the thread pool; publishing does not block on handler completion.
/// </summary>
public sealed class InMemoryServerEventTransport : IServerEventTransport
{
    private sealed class Subscription : IDisposable
    {
        private readonly InMemoryServerEventTransport _owner;
        private readonly string _key;
        private readonly Func<ReadOnlyMemory<byte>, IReadOnlyDictionary<string, string>, CancellationToken, Task> _handler;
        private int _disposed;

        public Subscription(InMemoryServerEventTransport owner, string key, Func<ReadOnlyMemory<byte>, IReadOnlyDictionary<string, string>, CancellationToken, Task> handler)
        {
            _owner = owner; _key = key; _handler = handler;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            _owner.Unsubscribe(_key, _handler);
        }
    }

    private readonly ConcurrentDictionary<string, List<Func<ReadOnlyMemory<byte>, IReadOnlyDictionary<string, string>, CancellationToken, Task>>> _subs = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public Task PublishAsync(string eventKey, ReadOnlyMemory<byte> payload, IReadOnlyDictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        if (eventKey is null) throw new ArgumentNullException(nameof(eventKey));
        if (_subs.TryGetValue(eventKey, out var handlers))
        {
            List<Func<ReadOnlyMemory<byte>, IReadOnlyDictionary<string, string>, CancellationToken, Task>> snapshot;
            lock (_gate) { snapshot = handlers.ToList(); }
            foreach (var h in snapshot)
            {
                // Fire-and-forget on thread pool; transport is best-effort in-memory
                _ = Task.Run(() => h(payload, headers ?? new Dictionary<string, string>(), ct), ct);
            }
        }
        return Task.CompletedTask;
    }

    public IDisposable Subscribe(string eventKey, Func<ReadOnlyMemory<byte>, IReadOnlyDictionary<string, string>, CancellationToken, Task> onMessage)
    {
        if (eventKey is null) throw new ArgumentNullException(nameof(eventKey));
        if (onMessage is null) throw new ArgumentNullException(nameof(onMessage));
        var list = _subs.GetOrAdd(eventKey, _ => new List<Func<ReadOnlyMemory<byte>, IReadOnlyDictionary<string, string>, CancellationToken, Task>>());
        lock (_gate) { list.Add(onMessage); }
        return new Subscription(this, eventKey, onMessage);
    }

    private void Unsubscribe(string eventKey, Func<ReadOnlyMemory<byte>, IReadOnlyDictionary<string, string>, CancellationToken, Task> handler)
    {
        if (_subs.TryGetValue(eventKey, out var list))
        {
            lock (_gate)
            {
                list.Remove(handler);
                if (list.Count == 0) _subs.TryRemove(eventKey, out _);
            }
        }
    }
}

public static class ServerEventTransportServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory server event transport.
    /// </summary>
    public static IServiceCollection AddInMemoryServerEventTransport(this IServiceCollection services)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        return services.AddSingleton<IServerEventTransport, InMemoryServerEventTransport>();
    }
}
