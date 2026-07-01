using System.Linq;
using Swap.Htmx.Fragments;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapFragmentRegistryTests
{
    [Fact]
    public void ResolveForTopics_ReturnsDependentFragments_Deduped_InRegistrationOrder()
    {
        var registry = new SwapFragmentRegistry();
        registry.Fragment("revenue", "_Revenue", _ => 1).DependsOn("orders", "refunds");
        registry.Fragment("order-list", "_OrderList", _ => 2).DependsOn("orders");
        registry.Fragment("users", "_Users", _ => 3).DependsOn("users");

        // Invalidating two topics that both hit "revenue" must still render it once.
        var fired = registry.ResolveForTopics(new[] { "orders", "refunds" }).Select(f => f.Id).ToArray();

        Assert.Equal(new[] { "revenue", "order-list" }, fired);
    }

    [Fact]
    public void ResolveForTopics_ExcludesUnrelatedFragments()
    {
        var registry = new SwapFragmentRegistry();
        registry.Fragment("revenue", "_Revenue", _ => 1).DependsOn("orders");
        registry.Fragment("users", "_Users", _ => 2).DependsOn("users");

        var fired = registry.ResolveForTopics(new[] { "orders" }).Select(f => f.Id).ToArray();

        Assert.Equal(new[] { "revenue" }, fired);
    }

    [Fact]
    public void Fragment_DuplicateId_Throws()
    {
        var registry = new SwapFragmentRegistry();
        registry.Fragment("x", "_X", _ => 1);
        Assert.Throws<System.InvalidOperationException>(() => registry.Fragment("x", "_X2", _ => 2));
    }
}
