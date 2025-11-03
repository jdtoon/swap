using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Swap.Htmx.Events;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapEventResolutionModeTests
{
    private static DefaultHttpContext Ctx()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    [Fact]
    public void OneHop_Does_Not_Reverse()
    {
        var context = Ctx();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions
        {
            ResolutionMode = ChainResolutionMode.OneHop
        }.Chain("a.b", "b.c");

        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        bus.Emit("b.c");

        var (resolved, _) = bus.ResolveAndFilterFor(context);
        Assert.Contains("b.c", resolved.Keys);
        Assert.DoesNotContain("a.b", resolved.Keys);
    }

    [Fact]
    public void Bidirectional_Includes_Reverse_OneHop()
    {
        var context = Ctx();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions
        {
            ResolutionMode = ChainResolutionMode.Bidirectional
        }.Chain("a.b", "b.c");

        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        bus.Emit("b.c");

        var (resolved, _) = bus.ResolveAndFilterFor(context);
        Assert.Contains("b.c", resolved.Keys);
        Assert.Contains("a.b", resolved.Keys); // reverse one-hop
    }

    [Fact]
    public void Transitive_Expands_To_Depth_2()
    {
        var context = Ctx();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions
        {
            ResolutionMode = ChainResolutionMode.Transitive,
            MaxTransitiveDepth = 2
        }
        .Chain("a", "b")
        .Chain("b", "c")
        .Chain("c", "d");

        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        bus.Emit("a");

        var (resolved, _) = bus.ResolveAndFilterFor(context);
        Assert.Contains("a", resolved.Keys);
        Assert.Contains("b", resolved.Keys);
        Assert.Contains("c", resolved.Keys); // depth 2 includes c
        Assert.DoesNotContain("d", resolved.Keys); // beyond depth 2
    }

    [Fact]
    public void Transitive_Depth_1_Equals_OneHop()
    {
        var context = Ctx();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions
        {
            ResolutionMode = ChainResolutionMode.Transitive,
            MaxTransitiveDepth = 1
        }
        .Chain("a", "b")
        .Chain("b", "c");

        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        bus.Emit("a");

        var (resolved, _) = bus.ResolveAndFilterFor(context);
        Assert.Contains("a", resolved.Keys);
        Assert.Contains("b", resolved.Keys);
        Assert.DoesNotContain("c", resolved.Keys);
    }

    [Fact]
    public void OneHop_Fanout_Over_Eight()
    {
        // Arrange: root -> e1..e9 (9 children)
        var context = Ctx();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions { ResolutionMode = ChainResolutionMode.OneHop };
        var children = Enumerable.Range(1, 9).Select(i => $"e{i}").ToArray();
        options.Chain("root", children);

        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        bus.Emit("root");

        // Act
        var (resolved, _) = bus.ResolveAndFilterFor(context);

        // Assert: includes root and all 9 children; not include any reverse or transitive
        Assert.Contains("root", resolved.Keys);
        foreach (var ch in children)
        {
            Assert.Contains(ch, resolved.Keys);
        }
    }

    [Fact]
    public void Bidirectional_Reverse_Gathers_Many_Parents()
    {
        // Arrange: t1..t9 -> hub; emit hub; bidirectional should include all t1..t9
        var context = Ctx();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions { ResolutionMode = ChainResolutionMode.Bidirectional };
        for (int i = 1; i <= 9; i++)
        {
            options.Chain($"t{i}", "hub");
        }

        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        bus.Emit("hub");

        // Act
        var (resolved, _) = bus.ResolveAndFilterFor(context);

        // Assert: hub plus all parents
        Assert.Contains("hub", resolved.Keys);
        for (int i = 1; i <= 9; i++)
        {
            Assert.Contains($"t{i}", resolved.Keys);
        }
    }

    [Fact]
    public void Transitive_Deep_Chain_Over_Eight()
    {
        // Arrange: a->b->c->d->e->f->g->h->i->j (10 nodes), depth 9 to reach j
        var context = Ctx();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions { ResolutionMode = ChainResolutionMode.Transitive, MaxTransitiveDepth = 9 };
        var nodes = new[] { "a","b","c","d","e","f","g","h","i","j" };
        for (int k = 0; k < nodes.Length - 1; k++)
        {
            options.Chain(nodes[k], nodes[k + 1]);
        }

        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        bus.Emit("a");

        // Act
        var (resolved, _) = bus.ResolveAndFilterFor(context);

        // Assert: includes all nodes
        foreach (var n in nodes)
        {
            Assert.Contains(n, resolved.Keys);
        }
    }
}
