using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;

namespace Swap.Htmx.State;

/// <summary>
/// Shared utilities for rendering SwapState as HTML.
/// </summary>
internal static class SwapStateRenderer
{
    /// <summary>
    /// Renders a SwapState as an OOB swap element.
    /// </summary>
    /// <param name="state">The state to render.</param>
    /// <returns>HTML string with hx-swap-oob="true".</returns>
    public static string RenderAsOob(SwapState state)
    {
        var encoder = HtmlEncoder.Default;
        var sb = new StringBuilder();
        
        sb.Append($"<div id=\"{encoder.Encode(state.ContainerId)}\" hx-swap-oob=\"true\" style=\"display: none;\">");
        
        foreach (var kvp in state.GetStateValues())
        {
            var fieldValue = FormatValue(kvp.Value);
            sb.Append($"<input type=\"hidden\" name=\"{encoder.Encode(kvp.Key)}\" value=\"{encoder.Encode(fieldValue)}\" />");
        }
        
        sb.Append("</div>");
        
        return sb.ToString();
    }

    /// <summary>
    /// Formats a value for HTML output.
    /// </summary>
    public static string FormatValue(object? value)
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
