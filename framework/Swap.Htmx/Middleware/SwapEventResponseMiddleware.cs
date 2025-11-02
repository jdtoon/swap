using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Swap.Htmx.Events;

namespace Swap.Htmx.Middleware;

/// <summary>
/// Middleware that builds HX-Trigger headers at the end of the request from pending events.
/// Should be registered before MVC endpoints; uses OnStarting to append headers safely.
/// </summary>
public class SwapEventResponseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHttpContextAccessor _http;
    private readonly SwapEventBusOptions _options;
    private readonly ILogger<SwapEventResponseMiddleware>? _logger;

    public SwapEventResponseMiddleware(
        RequestDelegate next,
        IHttpContextAccessor http,
        SwapEventBusOptions options,
        ILogger<SwapEventResponseMiddleware>? logger = null)
    {
        _next = next;
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        var bus = new SwapEventBus(_http, _options, null);
        var (resolved, beforeFilter) = bus.ResolveAndFilterFor(context);

        if (resolved.Count == 0)
        {
            return;
        }

        var json = JsonSerializer.Serialize(resolved);
        if (!context.Response.Headers.TryGetValue("HX-Trigger", out StringValues existing) || StringValues.IsNullOrEmpty(existing))
        {
            context.Response.Headers["HX-Trigger"] = json;
        }
        else
        {
            // Merge if someone already set HX-Trigger earlier (best-effort)
            try
            {
                var merged = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                foreach (var value in existing)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(value)) continue;
                        var obj = JsonSerializer.Deserialize<Dictionary<string, object?>>(value);
                        if (obj is not null)
                        {
                            foreach (var kv in obj) merged[kv.Key] = kv.Value;
                        }
                    }
                    catch
                    {
                        // ignore unparsable existing value
                    }
                }

                foreach (var kv in resolved) merged[kv.Key] = kv.Value; // last-write-wins
                var mergedJson = JsonSerializer.Serialize(merged);
                context.Response.Headers["HX-Trigger"] = mergedJson;
            }
            catch
            {
                // If merging fails, fall back to appending another header value
                context.Response.Headers.Append("HX-Trigger", json);
            }
        }

        var active = context.Items[SwapEventKeys.ActiveEvents] as HashSet<string>;
        var activeCount = active?.Count ?? 0;
        _logger?.LogInformation("[SwapEvents] Emitted={Emitted} Filtered={Filtered} Active={Active}", beforeFilter, resolved.Count, activeCount);
        context.Items.Remove(SwapEventKeys.PendingEvents);
    }
}
