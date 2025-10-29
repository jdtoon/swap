using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Swap.Htmx;

/// <summary>
/// Extension methods for registering Swap.Htmx services and middleware.
/// </summary>
public static class SwapHtmxServiceExtensions
{
    /// <summary>
    /// Adds Swap.Htmx services to the service collection.
    /// Currently this is a placeholder for future services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddSwapHtmx();
    /// </code>
    /// </example>
    public static IServiceCollection AddSwapHtmx(this IServiceCollection services)
    {
        // Placeholder for future services
        // Could add things like:
        // - HTMX configuration options
        // - Custom header processors
        // - Request/response interceptors
        return services;
    }

    /// <summary>
    /// Adds the Swap HTMX shell middleware to the application pipeline.
    /// This middleware enforces partial view responses for HTMX requests
    /// and helps debug issues where full pages are accidentally returned.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <example>
    /// <code>
    /// app.UseSwapHtmxShell();
    /// </code>
    /// </example>
    /// <remarks>
    /// Add this middleware early in the pipeline, typically right after
    /// UseRouting() and before endpoint middleware.
    /// </remarks>
    public static IApplicationBuilder UseSwapHtmxShell(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SwapHtmxShellMiddleware>();
    }
}
