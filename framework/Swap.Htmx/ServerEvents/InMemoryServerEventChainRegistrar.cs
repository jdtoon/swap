using System.Collections.Concurrent;
using Swap.Modularity.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Swap.Htmx.ServerEvents;

/// <summary>
/// In-memory registrar for domain/server event chains used by modules.
/// Thread-safe and process-local; suitable for demos, tests, and simple apps.
/// </summary>
public sealed class InMemoryServerEventChainRegistrar : IEventChainRegistrar
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<Func<object, IServiceProvider, Task>>> _handlers =
        new(StringComparer.Ordinal);

    /// <inheritdoc />
    public void Register<TEvent>(string eventKey, Func<TEvent, IServiceProvider, Task> handler)
    {
        if (eventKey is null) throw new ArgumentNullException(nameof(eventKey));
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var bag = _handlers.GetOrAdd(eventKey, _ => new ConcurrentBag<Func<object, IServiceProvider, Task>>());
        bag.Add(async (o, sp) =>
        {
            if (o is TEvent t)
                await handler(t, sp).ConfigureAwait(false);
        });
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(string eventKey, TEvent payload, IServiceProvider services, CancellationToken ct = default)
    {
        if (eventKey is null) throw new ArgumentNullException(nameof(eventKey));
        if (services is null) throw new ArgumentNullException(nameof(services));

        if (_handlers.TryGetValue(eventKey, out var bag))
        {
            foreach (var h in bag)
            {
                ct.ThrowIfCancellationRequested();
                await h(payload!, services).ConfigureAwait(false);
            }
        }
    }
}

public static class ServerEventChainServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory server event chain registrar implementing <see cref="IEventChainRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddSwapServerEventChains(this IServiceCollection services)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        return services.AddSingleton<IEventChainRegistrar, InMemoryServerEventChainRegistrar>();
    }
}
