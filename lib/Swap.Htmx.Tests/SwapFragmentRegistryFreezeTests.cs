using System;
using Swap.Htmx.Fragments;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapFragmentRegistryFreezeTests
{
    [Fact]
    public void Fragment_AfterFreeze_Throws()
    {
        var registry = new SwapFragmentRegistry();
        registry.Fragment("a", "_A", _ => null);
        registry.Freeze();

        Assert.Throws<InvalidOperationException>(() => registry.Fragment("b", "_B", _ => null));
    }

    [Fact]
    public void DependsOn_AfterFreeze_Throws()
    {
        var registry = new SwapFragmentRegistry();
        var registration = registry.Fragment("a", "_A", _ => null);
        registry.Freeze();

        Assert.Throws<InvalidOperationException>(() => registration.DependsOn("orders"));
    }

    [Fact]
    public void Registration_BeforeFreeze_Succeeds()
    {
        var registry = new SwapFragmentRegistry();
        registry.Fragment("a", "_A", _ => null).DependsOn("orders");
        registry.Freeze();

        Assert.Single(registry.ResolveForTopics(new[] { "orders" }));
    }
}
