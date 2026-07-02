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
    public void Build_AddsSeqAndHash_WhenProvided()
    {
        var attrs = SwapOobAttributes.Build(SwapMode.OuterHTML, seq: 42, hash: "abc123");
        Assert.Contains("hx-swap-oob=\"true\"", attrs);
        Assert.Contains("data-swap-seq=\"42\"", attrs);
        Assert.Contains("data-swap-hash=\"abc123\"", attrs);
    }

    [Fact]
    public void Build_OmitsSeqAndHash_WhenNull()
    {
        var attrs = SwapOobAttributes.Build(SwapMode.OuterHTML);
        Assert.DoesNotContain("data-swap-seq", attrs);
        Assert.DoesNotContain("data-swap-hash", attrs);
    }

    [Fact]
    public void InjectStampsIfMissing_AddsBoth_ToSelfDeclaredOob()
    {
        var html = "<div id=\"cart\" hx-swap-oob=\"true\">x</div>";
        var result = SwapOobAttributes.InjectStampsIfMissing(html, 5, "h1");
        Assert.Contains("data-swap-seq=\"5\"", result);
        Assert.Contains("data-swap-hash=\"h1\"", result);
    }

    [Fact]
    public void InjectStampsIfMissing_HandlesBareAttribute()
    {
        var html = "<div id=\"cart\" hx-swap-oob>x</div>";
        Assert.Contains("hx-swap-oob data-swap-seq=\"9\"", SwapOobAttributes.InjectStampsIfMissing(html, 9, null));
    }

    [Fact]
    public void InjectStampsIfMissing_IsNoOp_WhenNull_AlreadyStamped_OrNoOob()
    {
        var stamped = "<div hx-swap-oob=\"true\" data-swap-seq=\"1\">x</div>";
        Assert.Equal(stamped, SwapOobAttributes.InjectStampsIfMissing(stamped, 2, null));

        var html = "<div hx-swap-oob=\"true\">x</div>";
        Assert.Equal(html, SwapOobAttributes.InjectStampsIfMissing(html, null, null));

        var noOob = "<div id=\"x\">y</div>";
        Assert.Equal(noOob, SwapOobAttributes.InjectStampsIfMissing(noOob, 3, "h"));
    }

    [Fact]
    public void ComputeContentHash_IsStable_AndContentSensitive()
    {
        Assert.Equal(
            SwapOobAttributes.ComputeContentHash("<p>hello</p>"),
            SwapOobAttributes.ComputeContentHash("<p>hello</p>"));
        Assert.NotEqual(
            SwapOobAttributes.ComputeContentHash("<p>hello</p>"),
            SwapOobAttributes.ComputeContentHash("<p>world</p>"));
    }

    [Fact]
    public void AlsoUpdate_SetsOobSeqAndFingerprint()
    {
        var builder = new SwapResponseBuilder()
            .AlsoUpdate("x", "_X", null, SwapMode.OuterHTML, seq: 7, fingerprint: true);

        Assert.Equal(7, builder.OobSwaps[0].Seq);
        Assert.True(builder.OobSwaps[0].Fingerprint);
    }
}
