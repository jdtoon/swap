using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Swap.Htmx.TagHelpers;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapFrameTagHelperTests
{
    private static string Render(SwapFrameTagHelper helper, string childHtml = "")
    {
        var ctx = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "id");
        var output = new TagHelperOutput("swap-frame", new TagHelperAttributeList(),
            (useCached, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        output.Attributes.SetAttribute("id", "cart");
        if (!string.IsNullOrEmpty(childHtml))
        {
            output.Content.SetHtmlContent(childHtml);
        }

        helper.Process(ctx, output);

        using var sw = new StringWriter();
        output.WriteTo(sw, System.Text.Encodings.Web.HtmlEncoder.Default);
        return sw.ToString();
    }

    [Fact]
    public void LazyFrame_WithSrc_RendersRevealedTrigger_AndPreservesContent()
    {
        var html = Render(new SwapFrameTagHelper { Src = "/cart", Loading = "lazy" }, childHtml: "<span>skeleton</span>");

        Assert.Contains("<div", html);
        Assert.Contains("id=\"cart\"", html);
        Assert.Contains("hx-get=\"/cart\"", html);
        Assert.Contains("hx-trigger=\"revealed once\"", html);
        Assert.Contains("hx-swap=\"innerHTML\"", html);
        Assert.Contains("hx-target=\"this\"", html);
        Assert.Contains("skeleton", html);
    }

    [Fact]
    public void EagerFrame_UsesLoadTrigger()
    {
        var html = Render(new SwapFrameTagHelper { Src = "/cart" });
        Assert.Contains("hx-trigger=\"load once\"", html);
    }

    [Fact]
    public void Frame_WithoutSrc_RendersPlainContainer()
    {
        var html = Render(new SwapFrameTagHelper());
        Assert.Contains("id=\"cart\"", html);
        Assert.DoesNotContain("hx-get", html);
    }

    [Fact]
    public void Frame_CustomTrigger_Overrides()
    {
        var html = Render(new SwapFrameTagHelper { Src = "/cart", Trigger = "click" });
        Assert.Contains("hx-trigger=\"click\"", html);
    }
}
