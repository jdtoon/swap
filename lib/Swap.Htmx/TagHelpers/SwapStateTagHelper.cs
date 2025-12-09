using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Swap.Htmx.State;
using System.Globalization;
using System.Text.Encodings.Web;

namespace Swap.Htmx.TagHelpers;

/// <summary>
/// Tag helper that renders a SwapState object as a hidden fields container.
/// </summary>
/// <remarks>
/// Usage in Razor views:
/// <code>
/// &lt;swap-state state="Model.State" /&gt;
/// </code>
/// 
/// Renders as:
/// <code>
/// &lt;div id="inventory-state" style="display: none;"&gt;
///     &lt;input type="hidden" name="Tab" value="all" /&gt;
///     &lt;input type="hidden" name="Page" value="1" /&gt;
/// &lt;/div&gt;
/// </code>
/// </remarks>
[HtmlTargetElement("swap-state", TagStructure = TagStructure.WithoutEndTag)]
public class SwapStateTagHelper : TagHelper
{
    private readonly HtmlEncoder _htmlEncoder;

    /// <summary>
    /// The SwapState instance to render.
    /// </summary>
    [HtmlAttributeName("state")]
    public SwapState? State { get; set; }

    /// <summary>
    /// Optional custom ID for the container. 
    /// If not set, uses State.ContainerId.
    /// </summary>
    [HtmlAttributeName("id")]
    public string? ContainerId { get; set; }

    /// <summary>
    /// Optional prefix for field names.
    /// </summary>
    [HtmlAttributeName("prefix")]
    public string? Prefix { get; set; }

    /// <summary>
    /// Whether to render the container div. Default is true.
    /// Set to false to render only the hidden inputs.
    /// </summary>
    [HtmlAttributeName("include-container")]
    public bool IncludeContainer { get; set; } = true;

    /// <summary>
    /// Gets or sets the ViewContext.
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext? ViewContext { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="SwapStateTagHelper"/>.
    /// </summary>
    public SwapStateTagHelper(HtmlEncoder htmlEncoder)
    {
        _htmlEncoder = htmlEncoder;
    }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (State == null)
        {
            output.SuppressOutput();
            return;
        }

        var id = ContainerId ?? State.ContainerId;
        var values = State.GetStateValues();

        if (IncludeContainer)
        {
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.SetAttribute("id", id);
            output.Attributes.SetAttribute("style", "display: none;");
        }
        else
        {
            output.TagName = null;
            output.TagMode = TagMode.StartTagAndEndTag;
        }

        // Render hidden inputs for each state property
        foreach (var kvp in values)
        {
            var fieldName = string.IsNullOrEmpty(Prefix) 
                ? kvp.Key 
                : $"{Prefix}.{kvp.Key}";

            var fieldValue = FormatValue(kvp.Value);
            
            output.Content.AppendHtml($"<input type=\"hidden\" name=\"{_htmlEncoder.Encode(fieldName)}\" value=\"{_htmlEncoder.Encode(fieldValue)}\" />\n");
        }
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            bool b => b ? "true" : "false",
            decimal d => d.ToString(CultureInfo.InvariantCulture),
            double dbl => dbl.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString(CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("O"),
            DateTimeOffset dto => dto.ToString("O"),
            Enum e => e.ToString(),
            _ => value.ToString() ?? string.Empty
        };
    }
}
