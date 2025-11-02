using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Swap.Htmx.Middleware;

/// <summary>
/// Middleware that extracts active event subscriptions from the X-Swap-Events header
/// and stores them in HttpContext for later filtering.
/// </summary>
public class SwapEventContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SwapEventContextMiddleware>? _logger;

    public SwapEventContextMiddleware(RequestDelegate next, ILogger<SwapEventContextMiddleware>? logger = null)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Swap-Events", out var header) && header.Count > 0)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in header)
            {
                foreach (var token in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!string.IsNullOrWhiteSpace(token)) set.Add(token);
                }
            }
            context.Items[Events.SwapEventKeys.ActiveEvents] = set;
            _logger?.LogDebug("[SwapEvents] Active subscriptions: {Count}", set.Count);
        }

        await _next(context);
    }
}
