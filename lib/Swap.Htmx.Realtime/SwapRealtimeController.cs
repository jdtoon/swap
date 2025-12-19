using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Swap.Htmx;

/// <summary>
/// Optional base controller that adds realtime helpers (SSE) on top of <see cref="SwapController"/>.
/// </summary>
public abstract class SwapRealtimeController : SwapController
{
    /// <summary>
    /// Creates a Server-Sent Events (SSE) connection for streaming real-time updates to the client.
    /// </summary>
    protected IActionResult ServerSentEvents(Func<Realtime.ServerSentEventStream, CancellationToken, Task> handler)
    {
        var logger = HttpContext.RequestServices.GetService<ILogger<Realtime.ServerSentEventsResult>>();
        return new Realtime.ServerSentEventsResult(handler, logger);
    }

    /// <summary>
    /// Creates an enhanced SSE connection with connection management, rooms, and event filtering.
    /// </summary>
    protected IActionResult ServerSentEvents(Func<Realtime.SseConnectionBuilder, CancellationToken, Task> handler)
    {
        // Store controller reference for partial view rendering
        HttpContext.Items["SwapController"] = this;
        return new Realtime.EnhancedServerSentEventsResult(handler);
    }
}
