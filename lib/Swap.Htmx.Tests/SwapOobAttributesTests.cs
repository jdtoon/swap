using Swap.Htmx.Models;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapOobAttributesTests
{
    [Fact]
    public void Build_EmitsHxSwapOobToken()
    {
        Assert.Equal("hx-swap-oob=\"innerHTML\"", SwapOobAttributes.Build(SwapMode.InnerHTML));
        Assert.Equal("hx-swap-oob=\"morph:outerHTML\"", SwapOobAttributes.Build(SwapMode.MorphOuter));
    }

    [Fact]
    public void Build_AddsDataSwapSeq_WhenProvided()
    {
        var attrs = SwapOobAttributes.Build(SwapMode.OuterHTML, seq: 42);
        Assert.Contains("hx-swap-oob=\"true\"", attrs);
        Assert.Contains("data-swap-seq=\"42\"", attrs);
    }

    [Fact]
    public void Build_AddsDataSwapHash_WhenProvided()
    {
        Assert.Contains("data-swap-hash=\"ab12\"", SwapOobAttributes.Build(SwapMode.OuterHTML, hash: "ab12"));
    }

    [Fact]
    public void Build_OmitsSeqAndHash_WhenNull()
    {
        var attrs = SwapOobAttributes.Build(SwapMode.OuterHTML);
        Assert.DoesNotContain("data-swap-seq", attrs);
        Assert.DoesNotContain("data-swap-hash", attrs);
    }

    [Fact]
    public void AlsoUpdate_WithSeq_SetsOobSeq()
    {
        var builder = new SwapResponseBuilder().AlsoUpdate("x", "_X", null, SwapMode.OuterHTML, seq: 7);
        Assert.Equal(7, builder.OobSwaps[0].Seq);
    }
}
