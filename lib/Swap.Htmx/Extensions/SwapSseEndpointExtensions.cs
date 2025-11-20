using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Swap.Htmx.ServerSentEvents;

namespace Swap.Htmx;

public static class SwapSseEndpointExtensions
{
    /// <summary>
    /// Maps an endpoint for Server-Sent Events (SSE) that automatically integrates with the Swap.Htmx connection registry.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>The endpoint convention builder.</returns>
    public static IEndpointConventionBuilder MapSwapSse(this IEndpointRouteBuilder endpoints, string pattern)
    {
        return endpoints.MapGet(pattern, async (HttpContext context, ISseConnectionRegistry registry) =>
        {
            var response = context.Response;

            // Set SSE headers
            response.StatusCode = 200;
            response.ContentType = "text/event-stream; charset=utf-8";
            response.Headers["Cache-Control"] = "no-cache";
            response.Headers["Connection"] = "keep-alive";
            response.Headers["X-Accel-Buffering"] = "no";

            // Disable buffering
            var bufferingFeature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
            bufferingFeature?.DisableBuffering();

            var ct = context.RequestAborted;
            var stream = new ServerSentEventStream(response, ct);
            var connectionId = Guid.NewGuid().ToString("N");
            var connection = new SseConnection(connectionId, stream, context);

            // Register connection
            registry.RegisterConnection(connection);

            try
            {
                // Handle query parameters for auto-subscription
                var rooms = context.Request.Query["rooms"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var room in rooms)
                {
                    connection.JoinRoom(room);
                }

                var events = context.Request.Query["events"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var evt in events)
                {
                    connection.SubscribeToEvent(evt);
                }

                // Keep alive loop
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(15), ct);
                    // Optional: Send heartbeat comment
                    // await stream.SendCommentAsync("ping");
                }
            }
            catch (OperationCanceledException)
            {
                // Normal disconnection
            }
            finally
            {
                registry.UnregisterConnection(connectionId);
            }
        });
    }
}
