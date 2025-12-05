using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Swap.Htmx.TagHelpers;

/// <summary>
/// Tag helper that renders navigation links with HTMX attributes.
/// Simplifies SPA-style navigation by auto-adding hx-get, hx-target, and hx-push-url.
/// </summary>
/// <remarks>
/// Usage in Razor views:
/// <code>
/// &lt;swap-nav to="/products"&gt;Products&lt;/swap-nav&gt;
/// &lt;swap-nav to="/cart" class="btn"&gt;Cart&lt;/swap-nav&gt;
/// &lt;swap-nav to="/search" hx-vals='{"q": "test"}'&gt;Search&lt;/swap-nav&gt;
/// </code>
/// 
/// Renders as:
/// <code>
/// &lt;a hx-get="/products" hx-target="#main-content" hx-push-url="true"&gt;Products&lt;/a&gt;
/// &lt;a hx-get="/cart" hx-target="#main-content" hx-push-url="true" class="btn"&gt;Cart&lt;/a&gt;
/// &lt;a hx-get="/search" hx-target="#main-content" hx-push-url="true" hx-vals='{"q": "test"}'&gt;Search&lt;/a&gt;
/// </code>
/// 
/// The tag helper automatically adds:
/// - hx-get from the "to" attribute
/// - hx-target from global configuration (default: #main-content)
/// - hx-push-url="true" for browser history (unless push-url="false")
/// 
/// All other HTML and HTMX attributes pass through unchanged.
/// </remarks>
[HtmlTargetElement("swap-nav")]
public class SwapNavTagHelper : TagHelper
{
    /// <summary>
    /// The URL/route to navigate to. Sets hx-get attribute.
    /// </summary>
    [HtmlAttributeName("to")]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Whether to push the URL to browser history. Default is true.
    /// Set to false for in-page updates that shouldn't change the URL.
    /// </summary>
    [HtmlAttributeName("push-url")]
    public bool PushUrl { get; set; } = true;

    /// <summary>
    /// Gets or sets the ViewContext.
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext? ViewContext { get; set; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(To))
        {
            output.SuppressOutput();
            return;
        }

        // Change tag to anchor
        output.TagName = "a";
        output.TagMode = TagMode.StartTagAndEndTag;

        // Set hx-get from "to" attribute
        output.Attributes.SetAttribute("hx-get", To);

        // Set hx-target if not already specified
        if (!context.AllAttributes.ContainsName("hx-target"))
        {
            var options = ViewContext?.HttpContext.RequestServices.GetService<SwapHtmxOptions>();
            var target = options?.DefaultNavigationTarget ?? "#main-content";
            output.Attributes.SetAttribute("hx-target", target);
        }

        // Set hx-push-url if not already specified and PushUrl is true
        if (!context.AllAttributes.ContainsName("hx-push-url"))
        {
            output.Attributes.SetAttribute("hx-push-url", PushUrl ? "true" : "false");
        }

        // Remove our custom attributes that shouldn't be in output
        output.Attributes.RemoveAll("to");
        output.Attributes.RemoveAll("push-url");
    }
}
