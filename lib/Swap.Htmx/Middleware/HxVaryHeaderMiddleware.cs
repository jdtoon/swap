using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Swap.Htmx.Middleware;

/// <summary>
/// Middleware that ensures responses vary on the HX-Request header so caches/CDNs
/// do not serve HTMX partials to full-page requests (and vice versa).
/// </summary>
public sealed class HxVaryHeaderMiddleware
{
    private readonly RequestDelegate _next;
    public HxVaryHeaderMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        // Set Vary early; downstream code can add other vary values, our helper de-duplicates
        context.Response.EnsureVaryHxRequest();
        await _next(context);
    }
}

public static class HxVaryHeaderMiddlewareExtensions
{
    /// <summary>
    /// Adds middleware that sets Vary: HX-Request on all responses.
    /// Safe for endpoints that don't diverge; required where HTMX vs full-page differs.
    /// </summary>
    public static IApplicationBuilder UseSwapHtmxVary(this IApplicationBuilder app)
        => app.UseMiddleware<HxVaryHeaderMiddleware>();
}
