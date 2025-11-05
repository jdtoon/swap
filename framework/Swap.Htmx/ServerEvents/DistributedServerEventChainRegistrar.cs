using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Swap.Modularity.Abstractions;

namespace Swap.Htmx.ServerEvents;

/// <summary>
/// A distributed implementation of <see cref="IEventChainRegistrar"/> that uses an <see cref="IServerEventTransport"/>
/// to publish and subscribe to server events across processes.
/// </summary>
public sealed class DistributedServerEventChainRegistrar : IEventChainRegistrar, IDisposable
{
    private const string ClrTypeHeader = "ClrType";

    private readonly IServiceProvider _rootServices;
    private readonly IServerEventTransport _transport;
    private readonly JsonSerializerOptions _jsonOptions;

    private sealed record HandlerReg(Type ExpectedType, Func<object, IServiceProvider, Task> Handler);

    private readonly ConcurrentDictionary<string, List<HandlerReg>> _handlers = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, IDisposable> _subscriptions = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public DistributedServerEventChainRegistrar(IServiceProvider rootServices, IServerEventTransport transport, JsonSerializerOptions? jsonOptions = null)
    {
        _rootServices = rootServices ?? throw new ArgumentNullException(nameof(rootServices));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public void Register<TEvent>(string eventKey, Func<TEvent, IServiceProvider, Task> handler)
    {
        if (eventKey is null) throw new ArgumentNullException(nameof(eventKey));
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var list = _handlers.GetOrAdd(eventKey, _ => new List<HandlerReg>());
        lock (_gate) { list.Add(new HandlerReg(typeof(TEvent), async (o, sp) => await handler((TEvent)o, sp))); }

        // Ensure a single subscription per event key
        _subscriptions.GetOrAdd(eventKey, key => _transport.Subscribe(key, async (bytes, headers, ct) =>
        {
            if (!_handlers.TryGetValue(key, out var regs)) return;
            List<HandlerReg> snapshot; lock (_gate) { snapshot = regs.ToList(); }

            // Try type-hint from headers
            headers ??= new Dictionary<string, string>();
            var typeName = headers.TryGetValue(ClrTypeHeader, out var tn) ? tn : null;
            Type? hinted = null;
            if (!string.IsNullOrWhiteSpace(typeName))
            {
                try { hinted = Type.GetType(typeName!, throwOnError: false); } catch { hinted = null; }
            }

            foreach (var r in snapshot)
            {
                object? payloadObj = null;
                try
                {
                    var targetType = hinted ?? r.ExpectedType;
                    payloadObj = JsonSerializer.Deserialize(bytes.Span, targetType, _jsonOptions);
                    if (payloadObj is null) continue;
                }
                catch { continue; }

                // Create a scope for handler execution
                using var scope = _rootServices.CreateScope();
                await r.Handler(payloadObj!, scope.ServiceProvider).ConfigureAwait(false);
            }
        }));
    }

    public Task PublishAsync<TEvent>(string eventKey, TEvent payload, IServiceProvider services, CancellationToken ct = default)
    {
        if (eventKey is null) throw new ArgumentNullException(nameof(eventKey));
        var headers = new Dictionary<string, string>
        {
            [ClrTypeHeader] = typeof(TEvent).AssemblyQualifiedName ?? typeof(TEvent).FullName ?? typeof(TEvent).Name
        };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, typeof(TEvent), _jsonOptions);
        return _transport.PublishAsync(eventKey, bytes, headers, ct);
    }

    public void Dispose()
    {
        foreach (var d in _subscriptions.Values) d.Dispose();
        _subscriptions.Clear();
    }
}

public static class DistributedServerEventChainsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the distributed event chain registrar. Requires an <see cref="IServerEventTransport"/> to be registered.
    /// </summary>
    public static IServiceCollection AddSwapServerEventChainsDistributed(this IServiceCollection services)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        services.AddOptions();
        services.AddSingleton<IEventChainRegistrar, DistributedServerEventChainRegistrar>();
        return services;
    }
}
