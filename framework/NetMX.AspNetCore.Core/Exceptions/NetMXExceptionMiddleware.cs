using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace NetMX.AspNetCore.Core.Exceptions;

/// <summary>
/// Global exception handling middleware for NetMX applications.
/// Catches exceptions and returns appropriate HTTP responses.
/// </summary>
public class NetMXExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<NetMXExceptionMiddleware> _logger;

    public NetMXExceptionMiddleware(RequestDelegate next, ILogger<NetMXExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            ArgumentException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException => HttpStatusCode.NotFound,
            NotImplementedException => HttpStatusCode.NotImplemented,
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = new
            {
                message = exception.Message,
                type = exception.GetType().Name,
                statusCode = (int)statusCode
            }
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}
