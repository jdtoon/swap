using System.Linq;
using Swap.Htmx.Models;
using Xunit;

namespace Swap.Htmx.Tests;

/// <summary>
/// Tests for DOM morphing (idiomorph) support: the SwapMode.Morph* tokens and the AlsoMorph builder.
/// </summary>
public class SwapMorphTests
{
    [Theory]
    [InlineData(SwapMode.MorphOuter, "morph:outerHTML")]
    [InlineData(SwapMode.MorphInner, "morph:innerHTML")]
    [InlineData(SwapMode.OuterHTML, "true")]
    [InlineData(SwapMode.InnerHTML, "innerHTML")]
    [InlineData(SwapMode.Delete, "delete")]
    public void ToOobSwapToken_MapsSwapModes(SwapMode mode, string expected)
    {
        Assert.Equal(expected, mode.ToOobSwapToken());
    }

    [Fact]
    public void AlsoMorph_AddsOuterMorphOobSwap()
    {
        var builder = new SwapResponseBuilder().AlsoMorph("revenue", "_Revenue");

        var oob = builder.OobSwaps.Single();
        Assert.Equal("revenue", oob.TargetId);
        Assert.Equal("_Revenue", oob.ViewName);
        Assert.Equal(SwapMode.MorphOuter, oob.SwapMode);
    }

    [Fact]
    public void AlsoMorph_InnerHtml_UsesMorphInner()
    {
        var builder = new SwapResponseBuilder().AlsoMorph("list", "_List", innerHtml: true);

        Assert.Equal(SwapMode.MorphInner, builder.OobSwaps.Single().SwapMode);
    }
}
