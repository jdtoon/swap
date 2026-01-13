using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
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
        var provider = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<IDataProtectionProvider>();

        var sb = new StringBuilder();
        sb.AppendLine($"<div id=\"{encoder.Encode(id)}\" style=\"display: none;\">");

        foreach (var kvp in values)
        {
            var fieldName = string.IsNullOrEmpty(prefix) 
                ? kvp.Key 
                : $"{prefix}.{kvp.Key}";

            var fieldValue = SwapStateRenderer.GetFormattedValue(state, kvp.Key, kvp.Value, provider);
            
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
        var provider = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<IDataProtectionProvider>();

        var sb = new StringBuilder();

        foreach (var kvp in values)
        {
            var fieldName = string.IsNullOrEmpty(prefix) 
                ? kvp.Key 
                : $"{prefix}.{kvp.Key}";

            var fieldValue = SwapStateRenderer.GetFormattedValue(state, kvp.Key, kvp.Value, provider);
            
            sb.AppendLine($"<input type=\"hidden\" name=\"{encoder.Encode(fieldName)}\" value=\"{encoder.Encode(fieldValue)}\" />");
        }

        return new HtmlString(sb.ToString());
    }

    /// <summary>
    /// Generates a query string from the current state values for URL synchronization.
    /// Handles encryption if the state is protected.
    /// </summary>
    /// <param name="htmlHelper">The HTML helper.</param>
    /// <param name="state">The SwapState instance.</param>
    /// <returns>A query string like "?Page=2&amp;Tab=active" or empty.</returns>
    public static string SwapStateQueryString(
        this IHtmlHelper htmlHelper,
        SwapState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var provider = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<IDataProtectionProvider>();
        return state.ToQueryString(provider);
    }
}
