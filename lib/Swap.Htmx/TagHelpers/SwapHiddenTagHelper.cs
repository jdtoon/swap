using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Collections;
using System.Globalization;
using System.Text.Encodings.Web;

namespace Swap.Htmx.TagHelpers;

/// <summary>
/// Tag helper that renders a properly formatted hidden input field.
/// Use this for colocated state that lives with the controls, not in a separate container.
/// </summary>
/// <remarks>
/// Usage in Razor views:
/// <code>
/// &lt;swap-hidden name="page" value="@Model.Page" /&gt;
/// &lt;swap-hidden name="startDate" value="@Model.StartDate" /&gt;
/// &lt;swap-hidden name="includeArchived" value="@Model.IncludeArchived" /&gt;
/// </code>
/// 
/// Renders as:
/// <code>
/// &lt;input type="hidden" name="page" value="1" /&gt;
/// &lt;input type="hidden" name="startDate" value="2025-01-15" /&gt;
/// &lt;input type="hidden" name="includeArchived" value="true" /&gt;
/// </code>
/// 
/// Key benefits over raw input elements:
/// - Automatic value formatting for dates, booleans, decimals, enums
/// - Consistent null handling
/// - Option to suppress empty values
/// - Collection support (renders comma-separated or multiple inputs)
/// </remarks>
[HtmlTargetElement("swap-hidden", TagStructure = TagStructure.WithoutEndTag)]
public class SwapHiddenTagHelper : TagHelper
{
    private readonly HtmlEncoder _htmlEncoder;

    /// <summary>
    /// The name attribute for the hidden input.
    /// </summary>
    [HtmlAttributeName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The value to render. Automatically formatted based on type.
    /// </summary>
    [HtmlAttributeName("value")]
    public object? Value { get; set; }

    /// <summary>
    /// Whether to render the input even when value is null or empty. Default is false.
    /// </summary>
    [HtmlAttributeName("include-empty")]
    public bool IncludeEmpty { get; set; } = false;

    /// <summary>
    /// For collection values, whether to render multiple inputs (true) or comma-separated (false).
    /// Default is false (comma-separated).
    /// </summary>
    [HtmlAttributeName("multiple")]
    public bool RenderMultiple { get; set; } = false;

    /// <summary>
    /// Optional date format string. Default is "yyyy-MM-dd" for dates.
    /// Use "O" for ISO 8601 format with time component.
    /// </summary>
    [HtmlAttributeName("date-format")]
    public string DateFormat { get; set; } = "yyyy-MM-dd";

    /// <summary>
    /// Initializes a new instance of <see cref="SwapHiddenTagHelper"/>.
    /// </summary>
    public SwapHiddenTagHelper(HtmlEncoder htmlEncoder)
    {
        _htmlEncoder = htmlEncoder;
    }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrEmpty(Name))
        {
            output.SuppressOutput();
            return;
        }

        var stringValue = FormatValue(Value);

        // Suppress output for empty values unless explicitly included
        if (string.IsNullOrEmpty(stringValue) && !IncludeEmpty)
        {
            output.SuppressOutput();
            return;
        }

        // Handle collections
        if (Value is IEnumerable enumerable && Value is not string)
        {
            if (RenderMultiple)
            {
                // Render multiple inputs with same name
                output.TagName = null;
                foreach (var item in enumerable)
                {
                    var itemValue = FormatSingleValue(item);
                    output.Content.AppendHtml(
                        $"<input type=\"hidden\" name=\"{_htmlEncoder.Encode(Name)}\" value=\"{_htmlEncoder.Encode(itemValue)}\" />\n");
                }
                return;
            }
            // Otherwise fall through to render as comma-separated
        }

        output.TagName = "input";
        output.TagMode = TagMode.SelfClosing;
        output.Attributes.SetAttribute("type", "hidden");
        output.Attributes.SetAttribute("name", Name);
        output.Attributes.SetAttribute("value", stringValue);
    }

    private string FormatValue(object? value)
    {
        if (value == null)
            return string.Empty;

        // Handle collections as comma-separated
        if (value is IEnumerable enumerable && value is not string)
        {
            var items = new List<string>();
            foreach (var item in enumerable)
            {
                var itemValue = FormatSingleValue(item);
                if (!string.IsNullOrEmpty(itemValue))
                    items.Add(itemValue);
            }
            return string.Join(",", items);
        }

        return FormatSingleValue(value);
    }

    private string FormatSingleValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            bool b => b ? "true" : "false",
            DateTime dt => dt.ToString(DateFormat, CultureInfo.InvariantCulture),
            DateOnly d => d.ToString(DateFormat, CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString(DateFormat, CultureInfo.InvariantCulture),
            decimal dec => dec.ToString(CultureInfo.InvariantCulture),
            double dbl => dbl.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString(CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            long l => l.ToString(CultureInfo.InvariantCulture),
            Guid g => g.ToString(),
            Enum e => e.ToString(),
            _ => value.ToString() ?? string.Empty
        };
    }
}
