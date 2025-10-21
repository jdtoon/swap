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
            RequestId = Guid.NewGuid(), // Use new GUID for each request
            SessionId = GetSessionId(context),
            UserId = GetUserId(context)
        };

        // 2. Store EventContext in HttpContext.Items for controllers/services
        context.Items["NetMX.EventContext"] = eventContext;

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
    /// Extracts session ID from HTTP context (if sessions enabled).
    /// </summary>
    private static string GetSessionId(HttpContext context)
    {
        try
        {
            // Check if session is available
            if (context.Session != null && context.Session.IsAvailable)
            {
                return context.Session.Id;
            }
        }
        catch
        {
            // Session might not be configured, ignore
        }

        return string.Empty;
    }

    /// <summary>
    /// Extracts user ID from HTTP context (if authenticated).
    /// </summary>
    private static Guid? GetUserId(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
            return null;

        // Try multiple claim types (OpenID Connect, ASP.NET Identity, custom)
        var userIdClaim = context.User.FindFirst("sub")
            ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? context.User.FindFirst("userId");

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }
}
