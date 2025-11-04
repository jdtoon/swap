using System.Collections.Concurrent;
using Swap.Modularity.Abstractions;

namespace ModularMonolithDemo.Web;

public sealed class SimpleEventChainRegistrar : IEventChainRegistrar
{
    private readonly ConcurrentDictionary<string, List<Func<object, IServiceProvider, Task>>> _handlers = new(StringComparer.Ordinal);

    public void Register<TEvent>(string eventKey, Func<TEvent, IServiceProvider, Task> handler)
    {
        var list = _handlers.GetOrAdd(eventKey, _ => new List<Func<object, IServiceProvider, Task>>());
        list.Add(async (o, sp) =>
        {
            if (o is TEvent t)
                await handler(t, sp);
        });
    }

    public async Task PublishAsync<TEvent>(string eventKey, TEvent payload, IServiceProvider services, CancellationToken ct = default)
    {
        if (_handlers.TryGetValue(eventKey, out var list))
        {
            foreach (var h in list)
            {
                ct.ThrowIfCancellationRequested();
                await h(payload!, services);
            }
        }
    }
}
