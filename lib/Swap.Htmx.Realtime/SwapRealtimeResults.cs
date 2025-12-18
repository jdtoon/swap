using Microsoft.AspNetCore.Http;
using Swap.Htmx.Realtime;
using Swap.Htmx.Results;

namespace Swap.Htmx;

/// <summary>
/// Static entry point for Minimal APIs realtime results.
/// </summary>
public static class SwapRealtimeResults
{
    /// <summary>
    /// Creates a Server-Sent Events (SSE) connection result.
    /// </summary>
    public static IResult Sse(ISseConnectionRegistry registry, Action<SwapSseOptions>? configure = null)
    {
        return new SwapSseResult(registry, configure);
    }

    /// <summary>
    /// Creates a WebSocket connection result.
    /// </summary>
    public static IResult WebSocket(IRealtimeConnectionRegistry registry, Action<SwapWebSocketOptions>? configure = null)
    {
        return new SwapWebSocketResult(registry, configure);
    }
}
