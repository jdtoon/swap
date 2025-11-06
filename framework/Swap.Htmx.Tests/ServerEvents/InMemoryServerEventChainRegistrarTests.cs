using Swap.Htmx.ServerEvents;
using Swap.Modularity.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Swap.Htmx.Tests.ServerEvents;

public class InMemoryServerEventChainRegistrarTests
{
    private static (IServiceProvider sp, IEventChainRegistrar registrar) MakeProvider()
    {
        var services = new ServiceCollection();
        services.AddSwapServerEventChains();
        var sp = services.BuildServiceProvider();
        return (sp, sp.GetRequiredService<IEventChainRegistrar>());
    }

    [Fact]
    public async Task RegisterAndPublish_InvokesHandlers()
    {
        var (sp, reg) = MakeProvider();
        var calls = new List<string>();

        reg.Register<string>("E1", async (s, _) => { calls.Add("A:" + s); await Task.CompletedTask; });
        reg.Register<string>("E1", async (s, _) => { calls.Add("B:" + s); await Task.CompletedTask; });

        await reg.PublishAsync("E1", "hello", sp);

        Assert.Equal(2, calls.Count);
        Assert.Contains("A:hello", calls);
        Assert.Contains("B:hello", calls);
    }

    [Fact]
    public async Task Publish_WrongType_DoesNotInvoke()
    {
        var (sp, reg) = MakeProvider();
        var called = false;

        reg.Register<int>("E2", async (i, _) => { called = true; await Task.CompletedTask; });
        await reg.PublishAsync("E2", "not-int", sp);

        Assert.False(called);
    }

    [Fact]
    public async Task Cancellation_PreventsFurtherHandlers()
    {
        var (sp, reg) = MakeProvider();
        var count = 0;

        reg.Register<string>("E3", async (_, __) => { Interlocked.Increment(ref count); await Task.CompletedTask; });
        reg.Register<string>("E3", async (_, __) => { Interlocked.Increment(ref count); await Task.CompletedTask; });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await reg.PublishAsync("E3", "x", sp, cts.Token);
        });

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task MultipleEventKeys_AreIsolated()
    {
        var (sp, reg) = MakeProvider();
        var a = 0; var b = 0;
        reg.Register<string>("A", async (_, __) => { a++; await Task.CompletedTask; });
        reg.Register<string>("B", async (_, __) => { b++; await Task.CompletedTask; });

        await reg.PublishAsync("A", "x", sp);
        Assert.Equal(1, a);
        Assert.Equal(0, b);
    }
}
