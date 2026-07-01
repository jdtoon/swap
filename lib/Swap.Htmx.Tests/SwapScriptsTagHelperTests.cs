using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Swap.Htmx.TagHelpers;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapScriptsTagHelperTests
{
    private sealed class FakeEnv : IWebHostEnvironment
    {
        public FakeEnv(string env) => EnvironmentName = env;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Test";
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static string Render(SwapScriptsTagHelper helper, string env = "Production")
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWebHostEnvironment>(new FakeEnv(env));
        var http = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        helper.ViewContext = new ViewContext { HttpContext = http };

        var ctx = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "test-id");
        var output = new TagHelperOutput("swap-scripts", new TagHelperAttributeList(),
            (useCached, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        helper.Process(ctx, output);

        using var sw = new StringWriter();
        output.WriteTo(sw, System.Text.Encodings.Web.HtmlEncoder.Default);
        return sw.ToString();
    }

    [Fact]
    public void Renders_Htmx_And_SwapClient_ByDefault()
    {
        var html = Render(new SwapScriptsTagHelper());
        Assert.Contains("htmx.org", html);
        Assert.Contains("/_content/Swap.Htmx/js/swap.client.js", html);
        Assert.DoesNotContain("idiomorph", html);
        Assert.DoesNotContain("swap.devtools.js", html);
    }

    [Fact]
    public void Renders_Idiomorph_WhenMorphEnabled()
    {
        var html = Render(new SwapScriptsTagHelper { Morph = true });
        Assert.Contains("idiomorph", html);
    }

    [Fact]
    public void Renders_DevTools_InDevelopment()
    {
        var html = Render(new SwapScriptsTagHelper(), env: "Development");
        Assert.Contains("swap.devtools.js", html);
    }

    [Fact]
    public void HtmxSrc_IsOverridable()
    {
        var html = Render(new SwapScriptsTagHelper { HtmxSrc = "/lib/htmx/dist/htmx.min.js" });
        Assert.Contains("/lib/htmx/dist/htmx.min.js", html);
        Assert.DoesNotContain("unpkg.com/htmx", html);
    }
}
