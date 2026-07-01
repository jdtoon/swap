using Microsoft.AspNetCore.Http;
using Swap.Htmx.Fragments;
using Xunit;

namespace Swap.Htmx.Tests;

public class FragmentResolverTests
{
    [Fact]
    public void Resolve_ProducesOob_ForInvalidatedFragments_ExcludingExplicitTargets()
    {
        var registry = new SwapFragmentRegistry();
        registry.Fragment("revenue", "_Revenue", _ => "r").DependsOn("orders");
        registry.Fragment("order-list", "_OrderList", _ => "o").DependsOn("orders");

        // "order-list" is already an explicit AlsoUpdate target -> coalesced out.
        var swaps = FragmentResolver.Resolve(
            registry, new[] { "orders" }, new[] { "order-list" }, new DefaultHttpContext());

        Assert.Single(swaps);
        Assert.Equal("revenue", swaps[0].TargetId);
        Assert.Equal("_Revenue", swaps[0].ViewName);
        Assert.Equal("r", swaps[0].Model);
    }

    [Fact]
    public void Resolve_NoTopics_ReturnsEmpty()
    {
        var registry = new SwapFragmentRegistry();
        registry.Fragment("x", "_X", _ => 1).DependsOn("t");

        Assert.Empty(FragmentResolver.Resolve(registry, new string[0], new string[0], new DefaultHttpContext()));
    }
}
