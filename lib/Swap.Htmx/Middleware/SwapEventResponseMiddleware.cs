using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Swap.Htmx.Events;
using Swap.Htmx.Models;

namespace Swap.Htmx.Middleware;

/// <summary>
/// Middleware that builds <c>HX-Trigger</c> headers at the end of the request
/// from events captured in <see cref="SwapEventBus"/>. Register this early in
/// the pipeline so it can safely append headers before the response starts.
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
        // Capture flash toasts stashed by a PRIOR request (via WithFlash) before the endpoint runs, so
        // this request's own WithFlash writes defer to the next response. Emit them once (whichever of
        // OnStarting / the fallback runs first) so a redirect's success/error message shows on arrival.
        var tempData = context.RequestServices?.GetService<ITempDataDictionaryFactory>()?.GetTempData(context);
        var pendingFlash = tempData is not null
            ? SwapFlashHelper.TakePending(tempData)
            : (IReadOnlyList<ToastNotification>)Array.Empty<ToastNotification>();
        var flashEmitted = false;
        void EmitFlashOnce(HttpResponse response)
        {
            if (flashEmitted)
            {
                return;
            }

            flashEmitted = true;
            SwapFlashHelper.Emit(response, pendingFlash);
        }

        // Register an OnStarting callback so headers are set before the response starts
        context.Response.OnStarting(state =>
        {
            var httpContext = (HttpContext)state!;
            EmitFlashOnce(httpContext.Response);
            var bus = new SwapEventBus(_http, _options, null);
            var resolved = bus.ResolveChains(httpContext);

            if (resolved.Count == 0)
            {
                return Task.CompletedTask;
            }

            try
            {
                MergeHxTriggerHeader(httpContext.Response, resolved);

                _logger?.LogInformation("[SwapEvents] Resolved {Count} events for HX-Trigger", resolved.Count);
                httpContext.Items.Remove(SwapEventKeys.PendingEvents);
            }
            catch
            {
                // never throw from OnStarting
            }

            return Task.CompletedTask;
        }, context);

        await _next(context);

        // In unit-test scenarios or edge cases where OnStarting didn't run yet and the response hasn't started,
        // perform header setting now to ensure HX-Trigger is emitted.
        if (!context.Response.HasStarted)
        {
            EmitFlashOnce(context.Response);
            try
            {
                var bus = new SwapEventBus(_http, _options, null);
                var resolved = bus.ResolveChains(context);
                if (resolved.Count > 0)
                {
                    MergeHxTriggerHeader(context.Response, resolved);
                    _logger?.LogInformation("[SwapEvents] Resolved {Count} events for HX-Trigger", resolved.Count);
                }
            }
            catch
            {
                // swallow
            }
            finally
            {
                context.Items.Remove(SwapEventKeys.PendingEvents);
            }
        }
    }

    private static void MergeHxTriggerHeader(HttpResponse response, IReadOnlyDictionary<string, object?> resolved)
    {
        var json = JsonSerializer.Serialize(resolved);

        if (!response.Headers.TryGetValue("HX-Trigger", out StringValues existing) || StringValues.IsNullOrEmpty(existing))
        {
            response.Headers["HX-Trigger"] = json;
            return;
        }

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
            response.Headers["HX-Trigger"] = mergedJson;
        }
        catch
        {
            // If merging fails, fall back to appending another header value
            response.Headers.Append("HX-Trigger", json);
        }
    }
}
