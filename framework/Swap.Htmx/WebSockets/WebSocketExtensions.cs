using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Swap.Htmx.WebSockets;

/// <summary>
/// Extension methods for registering WebSocket handlers.
/// </summary>
public static class WebSocketExtensions
{
    /// <summary>
    /// Maps a WebSocket handler to a specific path.
    /// </summary>
    /// <typeparam name="THandler">The WebSocket handler type.</typeparam>
    /// <param name="app">The application builder.</param>
    /// <param name="path">The path to map the WebSocket to (e.g., "/ws/chat").</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder MapSwapWebSocket<THandler>(
        this IApplicationBuilder app,
        PathString path)
        where THandler : SwapWebSocketHandler
    {
        return app.Map(path, builder =>
        {
            builder.UseWebSockets();
            builder.Run(async context =>
            {
                var handler = ActivatorUtilities.CreateInstance<THandler>(context.RequestServices);
                await handler.HandleAsync(context);
            });
        });
    }
}
