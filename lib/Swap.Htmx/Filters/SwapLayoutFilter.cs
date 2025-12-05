using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace Swap.Htmx.Filters;

/// <summary>
/// Action filter that automatically suppresses layout for HTMX requests (non-boosted).
/// Eliminates the need for _ViewStart.cshtml layout logic in each module.
/// </summary>
/// <remarks>
/// Enable via configuration:
/// <code>
/// builder.Services.AddSwapHtmx(options => {
///     options.AutoSuppressLayout = true;
/// });
/// </code>
/// 
/// This filter checks for HX-Request header (but not HX-Boosted) and sets
/// a flag in HttpContext.Items that _ViewStart.cshtml can check:
/// <code>
/// @{ Layout = Context.Items["Swap.SuppressLayout"] is true ? null : "_Layout"; }
/// </code>
/// 
/// Or use the extension method:
/// <code>
/// @{ Layout = Context.ShouldSuppressLayout() ? null : "_Layout"; }
/// </code>
/// </remarks>
public class SwapLayoutFilter : IActionFilter
{
    /// <summary>
    /// Key used in HttpContext.Items to signal layout suppression.
    /// </summary>
    public const string SuppressLayoutKey = "Swap.SuppressLayout";

    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var options = context.HttpContext.RequestServices.GetService<SwapHtmxOptions>();
        
        // Only apply if auto-suppress is enabled
        if (options?.AutoSuppressLayout != true)
            return;

        // Check if this is an HTMX request (but not boosted)
        var request = context.HttpContext.Request;
        var isHtmxRequest = request.Headers.ContainsKey("HX-Request");
        var isBoosted = request.Headers.ContainsKey("HX-Boosted");

        if (isHtmxRequest && !isBoosted)
        {
            // Set flag that _ViewStart can check
            context.HttpContext.Items[SuppressLayoutKey] = true;
        }
    }

    /// <inheritdoc />
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No post-processing needed
    }
}
