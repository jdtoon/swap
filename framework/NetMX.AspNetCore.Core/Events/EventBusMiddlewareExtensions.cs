using Microsoft.AspNetCore.Builder;
using NetMX.AspNetCore.Core.Events;

namespace NetMX.AspNetCore.Core;

/// <summary>
/// Extension methods for registering Event Bus middleware.
/// </summary>
public static class EventBusMiddlewareExtensions
{
    /// <summary>
    /// Adds Event Bus middleware to the application pipeline.
    /// Must be called after UseSession() and before UseEndpoints().
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseEventBus(this IApplicationBuilder app)
    {
        return app.UseMiddleware<EventBusMiddleware>();
    }
}
