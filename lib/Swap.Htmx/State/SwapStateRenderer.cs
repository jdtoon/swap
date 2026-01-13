using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.DataProtection;

namespace Swap.Htmx.State;

/// <summary>
/// Shared utilities for rendering SwapState as HTML.
/// </summary>
internal static class SwapStateRenderer
{
    internal static bool IsPropertyProtected(SwapState state, string propertyName)
    {
        var prop = state.GetType().GetProperty(propertyName);
        if (prop == null) return state.Protected;

        // [SwapProtected] takes precedence -> True
        if (Attribute.IsDefined(prop, typeof(SwapProtectedAttribute))) return true;

        // [SwapUnprotected] takes precedence -> False
        if (Attribute.IsDefined(prop, typeof(SwapUnprotectedAttribute))) return false;

        // Default to class setting
        return state.Protected;
    }

    /// <summary>
    /// Renders a SwapState as an OOB swap element.
    /// </summary>
    /// <param name="state">The state to render.</param>
    /// <param name="protectionProvider">Optional data protection provider for encryption.</param>
    /// <returns>HTML string with hx-swap-oob="true".</returns>
    public static string RenderAsOob(SwapState state, IDataProtectionProvider? protectionProvider = null)
    {
        var encoder = HtmlEncoder.Default;
        var sb = new StringBuilder();
        
        sb.Append($"<div id=\"{encoder.Encode(state.ContainerId)}\" hx-swap-oob=\"true\" style=\"display: none;\">");
        
        foreach (var kvp in state.GetStateValues())
        {
            var fieldValue = GetFormattedValue(state, kvp.Key, kvp.Value, protectionProvider);
            sb.Append($"<input type=\"hidden\" name=\"{encoder.Encode(kvp.Key)}\" value=\"{encoder.Encode(fieldValue)}\" />");
        }
        
        sb.Append("</div>");
        
        return sb.ToString();
    }

    /// <summary>
    /// Formats a value for HTML output, optionally applying protection (encryption).
    /// </summary>
    public static string GetFormattedValue(SwapState state, string key, object? value, IDataProtectionProvider? protectionProvider)
    {
        var formatted = FormatValue(value);
        
        if (protectionProvider != null && IsPropertyProtected(state, key))
        {
            // Create a protector specific to this state container and property
            // This prevents copying values between different properties or different state objects
            var protector = protectionProvider.CreateProtector("SwapState", state.ContainerId, key);
            return protector.Protect(formatted);
        }
        
        return formatted;
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
