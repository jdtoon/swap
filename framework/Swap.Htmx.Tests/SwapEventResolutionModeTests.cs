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
}
