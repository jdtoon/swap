using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Swap.Htmx.Stories;

namespace Swap.Htmx;

/// <summary>
/// Extension methods for setting up SwapStories.
/// </summary>
public static class SwapStoriesExtensions
{
    private static SwapStoryRegistry? _registry;

    /// <summary>
    /// Adds SwapStories middleware to the pipeline.
    /// This middleware serves a component playground at /_swap/stories when in Development environment.
    /// </summary>
    /// <param name=""app"">The application builder.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseSwapStories(this IApplicationBuilder app)
    {
        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

        // Only run in development
        if (!env.IsDevelopment())
        {
            return app;
        }

        var partManager = app.ApplicationServices.GetRequiredService<ApplicationPartManager>();

        // Singleton registry instance
        _registry ??= new SwapStoryRegistry(partManager);

        return app.UseMiddleware<SwapStoriesMiddleware>(env, _registry);
    }
}
