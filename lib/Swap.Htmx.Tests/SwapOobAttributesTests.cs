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
    public void Build_OmitsSeq_WhenNull()
    {
        Assert.DoesNotContain("data-swap-seq", SwapOobAttributes.Build(SwapMode.OuterHTML));
    }

    [Fact]
    public void InjectSeqIfMissing_AddsSeq_ToSelfDeclaredOob()
    {
        var html = "<div id=\"cart\" hx-swap-oob=\"true\">x</div>";
        Assert.Contains("hx-swap-oob=\"true\" data-swap-seq=\"5\"", SwapOobAttributes.InjectSeqIfMissing(html, 5));
    }

    [Fact]
    public void InjectSeqIfMissing_HandlesBareAttribute()
    {
        var html = "<div id=\"cart\" hx-swap-oob>x</div>";
        Assert.Contains("hx-swap-oob data-swap-seq=\"9\"", SwapOobAttributes.InjectSeqIfMissing(html, 9));
    }

    [Fact]
    public void InjectSeqIfMissing_IsNoOp_WhenNull_AlreadyStamped_OrNoOob()
    {
        var stamped = "<div hx-swap-oob=\"true\" data-swap-seq=\"1\">x</div>";
        Assert.Equal(stamped, SwapOobAttributes.InjectSeqIfMissing(stamped, 2));

        var html = "<div hx-swap-oob=\"true\">x</div>";
        Assert.Equal(html, SwapOobAttributes.InjectSeqIfMissing(html, null));

        var noOob = "<div id=\"x\">y</div>";
        Assert.Equal(noOob, SwapOobAttributes.InjectSeqIfMissing(noOob, 3));
    }

    [Fact]
    public void AlsoUpdate_WithSeq_SetsOobSeq()
    {
        var builder = new SwapResponseBuilder().AlsoUpdate("x", "_X", null, SwapMode.OuterHTML, seq: 7);
        Assert.Equal(7, builder.OobSwaps[0].Seq);
    }
}
