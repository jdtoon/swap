using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Swap.Htmx.TagHelpers;

/// <summary>
/// Renders the client script block Swap.Htmx needs: htmx, the Swap client runtime, and (optionally)
/// the idiomorph morph extension, the SSE extension, and the dev tools. Removes the boilerplate of
/// hand-wiring <c>&lt;script&gt;</c> tags in every layout.
/// </summary>
/// <remarks>
/// Usage in <c>_Layout.cshtml</c> (typically at the end of <c>&lt;body&gt;</c>):
/// <code>
/// &lt;swap-scripts morph="true" /&gt;
/// </code>
/// To use <see cref="Swap.Htmx.Models.SwapMode.MorphInner"/>/<see cref="Swap.Htmx.Models.SwapMode.MorphOuter"/>
/// (or <c>AlsoMorph</c>), set <c>morph="true"</c> here and add <c>hx-ext="morph"</c> to your
/// <c>&lt;body&gt;</c> so htmx activates the idiomorph extension.
/// Vendoring your own scripts? Override any source with <c>htmx-src</c>, <c>idiomorph-src</c>, or <c>sse-src</c>.
/// </remarks>
[HtmlTargetElement("swap-scripts", TagStructure = TagStructure.WithoutEndTag)]
public class SwapScriptsTagHelper : TagHelper
{
    /// <summary>Source for the htmx script. Defaults to a pinned CDN build; override to vendor locally.</summary>
    [HtmlAttributeName("htmx-src")]
    public string HtmxSrc { get; set; } = "https://unpkg.com/htmx.org@2.0.4";

    /// <summary>Include the idiomorph extension so <c>morph</c> swap modes work. Default false.</summary>
    [HtmlAttributeName("morph")]
    public bool Morph { get; set; }

    /// <summary>Source for the idiomorph script (includes its htmx extension). Used when <c>morph</c> is true.</summary>
    [HtmlAttributeName("idiomorph-src")]
    public string IdiomorphSrc { get; set; } = "https://unpkg.com/idiomorph@0.7.3";

    /// <summary>Include the htmx Server-Sent Events extension. Default false.</summary>
    [HtmlAttributeName("sse")]
    public bool Sse { get; set; }

    /// <summary>Source for the htmx SSE extension script. Used when <c>sse</c> is true.</summary>
    [HtmlAttributeName("sse-src")]
    public string SseSrc { get; set; } = "https://unpkg.com/htmx-ext-sse@2.2.3";

    /// <summary>
    /// Include the Swap dev tools script. When null (default), it is included only in the Development
    /// environment.
    /// </summary>
    [HtmlAttributeName("devtools")]
    public bool? DevTools { get; set; }

    /// <summary>Gets or sets the ViewContext.</summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext? ViewContext { get; set; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var env = ViewContext?.HttpContext.RequestServices.GetService<IWebHostEnvironment>();
        var includeDevtools = DevTools ?? env?.IsDevelopment() ?? false;

        var sb = new StringBuilder();
        Script(sb, HtmxSrc);
        if (Sse)
        {
            Script(sb, SseSrc);
        }
        if (Morph)
        {
            Script(sb, IdiomorphSrc);
        }
        Script(sb, "/_content/Swap.Htmx/js/swap.client.js");
        if (includeDevtools)
        {
            Script(sb, "/_content/Swap.Htmx/js/swap.devtools.js");
        }

        // Render only the script tags — no wrapping <swap-scripts> element.
        output.TagName = null;
        output.Content.SetHtmlContent(sb.ToString());
    }

    private static void Script(StringBuilder sb, string src)
    {
        var encoded = System.Text.Encodings.Web.HtmlEncoder.Default.Encode(src);
        sb.Append("<script src=\"").Append(encoded).Append("\"></script>");
    }
}
