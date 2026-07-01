using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Swap.Htmx.TagHelpers;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapUploadTagHelperTests
{
    private static string Render(SwapUploadTagHelper helper, string childHtml = "")
    {
        var ctx = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "id");
        var output = new TagHelperOutput("swap-upload", new TagHelperAttributeList(),
            (useCached, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

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
    public void Upload_RendersFormWithHtmxAndProgress_AndPreservesContent()
    {
        var html = Render(
            new SwapUploadTagHelper { Name = "file", Url = "/upload", Target = "#result", Swap = "innerHTML" },
            childHtml: "<button>Upload</button>");

        Assert.Contains("<form", html);
        Assert.Contains("hx-post=\"/upload\"", html);
        Assert.Contains("hx-encoding=\"multipart/form-data\"", html);
        Assert.Contains("hx-target=\"#result\"", html);
        Assert.Contains("hx-swap=\"innerHTML\"", html);
        Assert.Contains("hx-on::xhr:progress=", html);
        Assert.Contains("event.detail.total", html);
        Assert.Contains("event.detail.loaded", html);
        Assert.Contains("<input type=\"file\" name=\"file\"", html);
        Assert.Contains("<progress value=\"0\" max=\"100\" style=\"display:none\">", html);
        Assert.Contains("<button>Upload</button>", html);
    }

    [Fact]
    public void Upload_DefaultsTargetToThis_AndSwapToInnerHtml()
    {
        var html = Render(new SwapUploadTagHelper { Name = "file", Url = "/upload" });

        Assert.Contains("hx-target=\"this\"", html);
        Assert.Contains("hx-swap=\"innerHTML\"", html);
    }

    [Fact]
    public void Upload_Multiple_AddsMultipleAttributeToInput()
    {
        var html = Render(new SwapUploadTagHelper { Name = "file", Url = "/upload", Multiple = true });

        Assert.Contains("multiple", html);
    }

    [Fact]
    public void Upload_NotMultiple_DoesNotAddMultipleAttribute()
    {
        var html = Render(new SwapUploadTagHelper { Name = "file", Url = "/upload", Multiple = false });

        Assert.DoesNotContain("multiple", html);
    }

    [Fact]
    public void Upload_MissingUrl_SuppressesOutput()
    {
        var html = Render(new SwapUploadTagHelper { Name = "file" });
        Assert.Equal(string.Empty, html);
    }

    [Fact]
    public void Upload_MissingName_SuppressesOutput()
    {
        var html = Render(new SwapUploadTagHelper { Url = "/upload" });
        Assert.Equal(string.Empty, html);
    }

    [Fact]
    public void Upload_StripsCustomAttributesFromForm()
    {
        var html = Render(new SwapUploadTagHelper { Name = "file", Url = "/upload" });

        Assert.DoesNotContain("name=\"file\" url", html);
        // Custom attributes should not leak onto the <form> element itself.
        var formTagEnd = html.IndexOf('>');
        var formTag = html.Substring(0, formTagEnd + 1);
        Assert.DoesNotContain(" name=", formTag);
        Assert.DoesNotContain(" url=", formTag);
        Assert.DoesNotContain(" multiple=", formTag);
    }
}
