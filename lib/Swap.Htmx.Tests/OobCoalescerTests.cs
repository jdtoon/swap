using Swap.Htmx.Models;
using Xunit;

namespace Swap.Htmx.Tests;

public class OobCoalescerTests
{
    [Fact]
    public void Duplicate_Replace_Swaps_To_Same_Target_Collapse_To_Last()
    {
        var builder = new SwapResponseBuilder()
            .WithView("_Main", null)
            .AlsoUpdate("#x", "_First", new { Value = 1 }, SwapMode.OuterHTML)
            .AlsoUpdate("#x", "_Second", new { Value = 2 }, SwapMode.OuterHTML);

        var coalesced = OobCoalescer.Coalesce(builder.OobSwaps);

        Assert.Single(coalesced);
        Assert.Equal("x", coalesced[0].TargetId);
        Assert.Equal("_Second", coalesced[0].ViewName);
    }

    [Fact]
    public void Swaps_To_Different_Targets_Are_All_Kept()
    {
        var builder = new SwapResponseBuilder()
            .WithView("_Main", null)
            .AlsoUpdate("#x", "_X", null, SwapMode.OuterHTML)
            .AlsoUpdate("#y", "_Y", null, SwapMode.OuterHTML)
            .AlsoUpdate("#z", "_Z", null, SwapMode.InnerHTML);

        var coalesced = OobCoalescer.Coalesce(builder.OobSwaps);

        Assert.Equal(3, coalesced.Count);
    }

    [Fact]
    public void Coalesced_Order_Preserves_Original_Relative_Order()
    {
        var builder = new SwapResponseBuilder()
            .WithView("_Main", null)
            .AlsoUpdate("#a", "_A", null, SwapMode.OuterHTML)
            .AlsoUpdate("#b", "_B1", null, SwapMode.OuterHTML)
            .AlsoUpdate("#b", "_B2", null, SwapMode.OuterHTML)
            .AlsoUpdate("#c", "_C", null, SwapMode.OuterHTML);

        var coalesced = OobCoalescer.Coalesce(builder.OobSwaps);

        Assert.Equal(3, coalesced.Count);
        Assert.Equal("a", coalesced[0].TargetId);
        Assert.Equal("b", coalesced[1].TargetId);
        Assert.Equal("_B2", coalesced[1].ViewName);
        Assert.Equal("c", coalesced[2].TargetId);
    }

    [Fact]
    public void Insert_Style_Swaps_To_Same_Target_All_Accumulate()
    {
        var builder = new SwapResponseBuilder()
            .WithView("_Main", null)
            .AlsoUpdate("#list", "_Item1", null, SwapMode.BeforeEnd)
            .AlsoUpdate("#list", "_Item2", null, SwapMode.BeforeEnd);

        var coalesced = OobCoalescer.Coalesce(builder.OobSwaps);

        Assert.Equal(2, coalesced.Count);
        Assert.Equal("_Item1", coalesced[0].ViewName);
        Assert.Equal("_Item2", coalesced[1].ViewName);
    }

    [Fact]
    public void Mixed_Replace_And_Insert_Swaps_To_Same_Target_Coalesce_Only_Replace()
    {
        var builder = new SwapResponseBuilder()
            .WithView("_Main", null)
            .AlsoUpdate("#panel", "_Old", null, SwapMode.OuterHTML)
            .AlsoUpdate("#panel", "_Insert1", null, SwapMode.BeforeEnd)
            .AlsoUpdate("#panel", "_New", null, SwapMode.OuterHTML)
            .AlsoUpdate("#panel", "_Insert2", null, SwapMode.AfterBegin);

        var coalesced = OobCoalescer.Coalesce(builder.OobSwaps);

        Assert.Equal(3, coalesced.Count);
        Assert.Equal("_Insert1", coalesced[0].ViewName);
        Assert.Equal("_New", coalesced[1].ViewName);
        Assert.Equal("_Insert2", coalesced[2].ViewName);
    }

    [Fact]
    public void Delete_And_Morph_Modes_Are_Treated_As_Replace_Style()
    {
        var builder = new SwapResponseBuilder()
            .WithView("_Main", null)
            .AlsoUpdate("#x", "_First", null, SwapMode.MorphOuter)
            .AlsoUpdate("#x", "_Second", null, SwapMode.Delete);

        var coalesced = OobCoalescer.Coalesce(builder.OobSwaps);

        Assert.Single(coalesced);
        Assert.Equal("_Second", coalesced[0].ViewName);
    }

    [Fact]
    public void Empty_List_Returns_Empty_List()
    {
        var coalesced = OobCoalescer.Coalesce(new List<OobSwap>());

        Assert.Empty(coalesced);
    }
}
