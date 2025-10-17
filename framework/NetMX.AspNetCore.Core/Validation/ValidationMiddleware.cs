using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NetMX.Ddd.Application.Validation;
using System.Text.Json;

namespace NetMX.AspNetCore.Core.Validation;

/// <summary>
/// Middleware that catches ValidationException and returns proper error responses.
/// Works seamlessly with HTMX requests.
/// </summary>
public class ValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ValidationMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationMiddleware"/> class.
    /// </summary>
    public ValidationMiddleware(RequestDelegate next, ILogger<ValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            await HandleValidationExceptionAsync(context, ex);
        }
    }

    private static async Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        context.Response.StatusCode = 400; // Bad Request
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = new
            {
                message = "Validation failed",
                type = "ValidationError",
                statusCode = 400,
                errors = exception.GetErrorsDictionary()
            }
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}
