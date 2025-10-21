using Microsoft.AspNetCore.Builder;
using NetMX.AspNetCore.Core.Events;

namespace NetMX.DependencyInjection;

/// <summary>
/// Extension methods for registering EventBus middleware.
/// </summary>
public static class EventBusApplicationBuilderExtensions
{
    /// <summary>
    /// Adds EventBus middleware to the application pipeline.
    /// This middleware:
    /// - Creates EventContext from HTTP request
    /// - Stores EventContext in HttpContext.Items
    /// - Injects HX-Trigger headers for triggered events
    /// 
    /// Must be called AFTER UseRouting() and BEFORE UseEndpoints().
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseEventBus(this IApplicationBuilder app)
    {
        return app.UseMiddleware<EventBusMiddleware>();
    }
}
