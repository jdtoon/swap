using Microsoft.AspNetCore.Http;
using Swap.Htmx.Models;
using Swap.Htmx.State;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapResponseBuilderAlsoUpdateTests
{
    [Fact]
    public void AlsoUpdateIfExists_Adds_Conditional_OobSwap()
    {
        var builder = new SwapResponseBuilder()
            .WithView("_Main", null)
            .AlsoUpdateIfExists("#sidebar", "_Sidebar", new { Count = 5 });

        var oobSwaps = builder.OobSwaps;
        
        Assert.Single(oobSwaps);
        Assert.Equal("#sidebar", oobSwaps[0].TargetId);
        Assert.Equal("_Sidebar", oobSwaps[0].ViewName);
        Assert.True(oobSwaps[0].ConditionalExists);
    }

    [Fact]
    public void AlsoUpdate_Default_Is_Not_Conditional()
    {
        var builder = new SwapResponseBuilder()
            .WithView("_Main", null)
            .AlsoUpdate("#sidebar", "_Sidebar");

        var oobSwaps = builder.OobSwaps;
        
        Assert.Single(oobSwaps);
        Assert.False(oobSwaps[0].ConditionalExists);
    }

    [Fact]
    public void AlsoUpdateIf_True_Condition_Adds_Swap()
    {
        var builder = new SwapResponseBuilder()
            .WithView("_Main", null)
            .AlsoUpdateIf(true, "#stats", "_Stats", new { Total = 100 });

        var oobSwaps = builder.OobSwaps;
        
        Assert.Single(oobSwaps);
        Assert.Equal("#stats", oobSwaps[0].TargetId);
    }

    [Fact]
    public void AlsoUpdateIf_False_Condition_Skips_Swap()
    {
        var builder = new SwapResponseBuilder()
            .WithView("_Main", null)
            .AlsoUpdateIf(false, "#stats", "_Stats", new { Total = 100 });

        var oobSwaps = builder.OobSwaps;
        
        Assert.Empty(oobSwaps);
    }

    [Fact]
    public void Can_Mix_Conditional_And_NonConditional_Swaps()
    {
        var hasPermission = true;
        var showAnalytics = false;

        var builder = new SwapResponseBuilder()
            .WithView("_Main", null)
            .AlsoUpdate("#header", "_Header")
            .AlsoUpdateIfExists("#sidebar", "_Sidebar")
            .AlsoUpdateIf(hasPermission, "#admin-panel", "_AdminPanel")
            .AlsoUpdateIf(showAnalytics, "#analytics", "_Analytics");

        var oobSwaps = builder.OobSwaps;
        
        Assert.Equal(3, oobSwaps.Count);
        
        // Regular swap
        Assert.Equal("#header", oobSwaps[0].TargetId);
        Assert.False(oobSwaps[0].ConditionalExists);
        
        // IfExists swap
        Assert.Equal("#sidebar", oobSwaps[1].TargetId);
        Assert.True(oobSwaps[1].ConditionalExists);
        
        // Conditional (true) swap
        Assert.Equal("#admin-panel", oobSwaps[2].TargetId);
        Assert.False(oobSwaps[2].ConditionalExists);
    }

    [Fact]
    public void AlsoUpdateIfExists_Supports_SwapMode()
    {
        var builder = new SwapResponseBuilder()
            .WithView("_Main", null)
            .AlsoUpdateIfExists("#content", "_Content", null, SwapMode.InnerHTML);

        var oobSwaps = builder.OobSwaps;
        
        Assert.Single(oobSwaps);
        Assert.Equal(SwapMode.InnerHTML, oobSwaps[0].SwapMode);
        Assert.True(oobSwaps[0].ConditionalExists);
    }

    [Fact]
    public void AlsoUpdateIf_Supports_SwapMode()
    {
        var builder = new SwapResponseBuilder()
            .WithView("_Main", null)
            .AlsoUpdateIf(true, "#content", "_Content", null, SwapMode.BeforeEnd);

        var oobSwaps = builder.OobSwaps;
        
        Assert.Single(oobSwaps);
        Assert.Equal(SwapMode.BeforeEnd, oobSwaps[0].SwapMode);
    }
}
