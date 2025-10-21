using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NetMX.Events;

namespace NetMX.AspNetCore.Core.Events;

/// <summary>
/// Middleware that integrates Event Bus with HTMX by automatically injecting HX-Trigger headers.
/// Creates EventContext from HTTP request and injects triggered events as HTMX headers in response.
/// </summary>
public class EventBusMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Creates a new EventBusMiddleware instance.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public EventBusMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Processes the HTTP request, creating EventContext and injecting HX-Trigger headers.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Create EventContext from HTTP request
        var eventContext = new EventContext
        {
            RequestId = Guid.TryParse(context.TraceIdentifier, out var traceId)
                ? traceId
                : Guid.NewGuid(),
            SessionId = context.Session.Id ?? string.Empty,
            UserId = GetUserId(context)
        };

        // 2. Store EventContext in HttpContext.Items for controllers/services
        context.Items["EventContext"] = eventContext;

        // 3. Execute the rest of the pipeline
        await _next(context);

        // 4. After request completes: Inject HX-Trigger headers (if any events triggered)
        var eventBus = context.RequestServices.GetService<IEventBus>();
        if (eventBus != null)
        {
            var triggeredEvents = eventBus.GetTriggeredEvents(eventContext.RequestId);
            if (triggeredEvents.Count > 0)
            {
                // Serialize events to JSON for HTMX
                var json = JsonSerializer.Serialize(triggeredEvents, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Add HX-Trigger header (HTMX will automatically handle these events)
                context.Response.Headers.Append("HX-Trigger", json);
            }
        }
    }

    /// <summary>
    /// Extracts user ID from HTTP context (if authenticated).
    /// </summary>
    private static Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }
}
