using Microsoft.Extensions.DependencyInjection;
using Swap.Htmx.ServerEvents;
using Swap.Modularity.Abstractions;
using Xunit;

namespace Swap.Htmx.Tests.ServerEvents;

public class DistributedServerEventChainRegistrarTests
{
    private static (IServiceProvider sp, IEventChainRegistrar reg) Make()
    {
        var sc = new ServiceCollection();
        sc.AddInMemoryServerEventTransport();
        sc.AddSwapServerEventChainsDistributed();
        var sp = sc.BuildServiceProvider();
        return (sp, sp.GetRequiredService<IEventChainRegistrar>());
    }

    [Fact]
    public async Task EndToEnd_DistributedRegistrar_Works_With_InMemoryTransport()
    {
        var (sp, reg) = Make();
        var calls = new List<int>();
        reg.Register<int>("K1", async (i, _) => { calls.Add(i); await Task.CompletedTask; });

        await reg.PublishAsync("K1", 42, sp);
        // Allow async fire-and-forget delivery
        await Task.Delay(50);
        Assert.Contains(42, calls);
    }

    [Fact]
    public async Task MultipleHandlers_SingleSubscription()
    {
        var (sp, reg) = Make();
        var a = 0; var b = 0;
        reg.Register<string>("K2", async (_, __) => { Interlocked.Increment(ref a); await Task.CompletedTask; });
        reg.Register<string>("K2", async (_, __) => { Interlocked.Increment(ref b); await Task.CompletedTask; });

        await reg.PublishAsync("K2", "x", sp);
        await Task.Delay(50);
        Assert.Equal(1, a);
        Assert.Equal(1, b);
    }
}
