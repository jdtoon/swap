using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Swap.Htmx.TagHelpers;

/// <summary>
/// Tag helper that renders a validation message placeholder for OOB swaps.
/// Works with <see cref="SwapValidationExtensions.SwapValidationErrors"/> to display field-level errors.
/// </summary>
/// <remarks>
/// Usage in Razor views:
/// <code>
/// &lt;input asp-for="ClientName" /&gt;
/// &lt;swap-validation for="ClientName" /&gt;
/// </code>
/// 
/// Renders as:
/// <code>
/// &lt;span id="swap-validation-ClientName" class="swap-validation-message"&gt;&lt;/span&gt;
/// </code>
/// 
/// When validation fails, the server sends an OOB swap to update this element.
/// </remarks>
[HtmlTargetElement("swap-validation", TagStructure = TagStructure.WithoutEndTag)]
public class SwapValidationTagHelper : TagHelper
{
    /// <summary>
    /// The model property name to display validation errors for.
    /// This should match the property name used in model binding.
    /// </summary>
    [HtmlAttributeName("for")]
    public string? For { get; set; }

    /// <summary>
    /// Optional CSS class to add. Defaults to "swap-validation-message".
    /// </summary>
    [HtmlAttributeName("class")]
    public string? CssClass { get; set; }

    /// <summary>
    /// Optional custom ID prefix. Defaults to "swap-validation-".
    /// </summary>
    [HtmlAttributeName("id-prefix")]
    public string IdPrefix { get; set; } = "swap-validation-";

    /// <summary>
    /// Gets or sets the ViewContext.
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext? ViewContext { get; set; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrEmpty(For))
        {
            output.SuppressOutput();
            return;
        }

        var id = $"{IdPrefix}{For}";
        var baseCssClass = CssClass ?? "swap-validation-message";

        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("id", id);
        output.Attributes.SetAttribute("data-swap-validation", For);

        // Check if there's an existing validation error in ModelState
        if (ViewContext?.ModelState.TryGetValue(For, out var entry) == true && entry.Errors.Count > 0)
        {
            output.Content.SetContent(entry.Errors[0].ErrorMessage);
            output.Attributes.SetAttribute("class", $"{baseCssClass} field-validation-error");
        }
        else
        {
            output.Attributes.SetAttribute("class", baseCssClass);
        }
    }
}
