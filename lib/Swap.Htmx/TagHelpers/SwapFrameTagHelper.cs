using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Swap.Htmx.TagHelpers;

/// <summary>
/// Renders a self-contained frame region (Turbo-Frame style): a container that lazily loads its own
/// content and scopes navigation to itself, without hand-wiring <c>hx-get</c>/<c>hx-trigger</c>/<c>hx-target</c>
/// on every control.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// &lt;swap-frame id="cart" src="/cart" loading="lazy"&gt;
///     &lt;div class="skeleton"&gt;Loading…&lt;/div&gt;
/// &lt;/swap-frame&gt;
/// </code>
/// Renders:
/// <code>
/// &lt;div id="cart" hx-get="/cart" hx-trigger="revealed once" hx-swap="innerHTML" hx-target="this"&gt;
///     &lt;div class="skeleton"&gt;Loading…&lt;/div&gt;
/// &lt;/div&gt;
/// </code>
/// With no <c>src</c>, it renders a plain identified container you can target from other swaps.
/// </remarks>
[HtmlTargetElement("swap-frame")]
public class SwapFrameTagHelper : TagHelper
{
    /// <summary>The URL the frame loads its content from. When omitted, renders a plain container.</summary>
    [HtmlAttributeName("src")]
    public string? Src { get; set; }

    /// <summary><c>lazy</c> loads when scrolled into view; <c>eager</c> (default) loads immediately.</summary>
    [HtmlAttributeName("loading")]
    public string Loading { get; set; } = "eager";

    /// <summary>Overrides the hx-trigger. Defaults to <c>revealed once</c> (lazy) or <c>load once</c> (eager).</summary>
    [HtmlAttributeName("trigger")]
    public string? Trigger { get; set; }

    /// <summary>The hx-swap style for the loaded content. Defaults to <c>innerHTML</c>.</summary>
    [HtmlAttributeName("swap")]
    public string Swap { get; set; } = "innerHTML";

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;

        if (!string.IsNullOrWhiteSpace(Src))
        {
            output.Attributes.SetAttribute("hx-get", Src);

            var trigger = string.IsNullOrWhiteSpace(Trigger)
                ? (Loading.Equals("lazy", StringComparison.OrdinalIgnoreCase) ? "revealed once" : "load once")
                : Trigger!;

            output.Attributes.SetAttribute("hx-trigger", trigger);
            output.Attributes.SetAttribute("hx-swap", Swap);

            // Scope navigation to the frame itself unless the author targeted elsewhere.
            if (!context.AllAttributes.ContainsName("hx-target"))
            {
                output.Attributes.SetAttribute("hx-target", "this");
            }
        }

        output.Attributes.RemoveAll("src");
        output.Attributes.RemoveAll("loading");
        output.Attributes.RemoveAll("trigger");
        output.Attributes.RemoveAll("swap");
        // Inner content (skeleton/placeholder) is preserved as-is.
    }
}
