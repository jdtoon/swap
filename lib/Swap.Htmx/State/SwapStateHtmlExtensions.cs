using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;

namespace Swap.Htmx.State;

/// <summary>
/// HTML helper extensions for rendering SwapState containers.
/// </summary>
public static class SwapStateHtmlExtensions
{
    /// <summary>
    /// Renders a SwapState object as a hidden fields container.
    /// </summary>
    /// <param name="htmlHelper">The HTML helper.</param>
    /// <param name="state">The SwapState instance to render.</param>
    /// <param name="containerId">Optional custom container ID.</param>
    /// <param name="prefix">Optional field name prefix.</param>
    /// <returns>HTML content representing the state container.</returns>
    /// <remarks>
    /// Usage in Razor views:
    /// <code>
    /// @Html.SwapStateContainer(Model.State)
    /// </code>
    /// 
    /// With custom options:
    /// <code>
    /// @Html.SwapStateContainer(Model.State, containerId: "my-state", prefix: "form")
    /// </code>
    /// </remarks>
    public static IHtmlContent SwapStateContainer(
        this IHtmlHelper htmlHelper, 
        SwapState state,
        string? containerId = null,
        string? prefix = null)
    {
        ArgumentNullException.ThrowIfNull(state);

        var id = containerId ?? state.ContainerId;
        var values = state.GetStateValues();
        var encoder = HtmlEncoder.Default;

        var sb = new StringBuilder();
        sb.AppendLine($"<div id=\"{encoder.Encode(id)}\" style=\"display: none;\">");

        foreach (var kvp in values)
        {
            var fieldName = string.IsNullOrEmpty(prefix) 
                ? kvp.Key 
                : $"{prefix}.{kvp.Key}";

            var fieldValue = FormatValue(kvp.Value);
            
            sb.AppendLine($"    <input type=\"hidden\" name=\"{encoder.Encode(fieldName)}\" value=\"{encoder.Encode(fieldValue)}\" />");
        }

        sb.AppendLine("</div>");

        return new HtmlString(sb.ToString());
    }

    /// <summary>
    /// Renders only the hidden input fields for a SwapState (without container div).
    /// </summary>
    /// <param name="htmlHelper">The HTML helper.</param>
    /// <param name="state">The SwapState instance to render.</param>
    /// <param name="prefix">Optional field name prefix.</param>
    /// <returns>HTML content representing the hidden fields.</returns>
    public static IHtmlContent SwapStateFields(
        this IHtmlHelper htmlHelper,
        SwapState state,
        string? prefix = null)
    {
        ArgumentNullException.ThrowIfNull(state);

        var values = state.GetStateValues();
        var encoder = HtmlEncoder.Default;

        var sb = new StringBuilder();

        foreach (var kvp in values)
        {
            var fieldName = string.IsNullOrEmpty(prefix) 
                ? kvp.Key 
                : $"{prefix}.{kvp.Key}";

            var fieldValue = FormatValue(kvp.Value);
            
            sb.AppendLine($"<input type=\"hidden\" name=\"{encoder.Encode(fieldName)}\" value=\"{encoder.Encode(fieldValue)}\" />");
        }

        return new HtmlString(sb.ToString());
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
