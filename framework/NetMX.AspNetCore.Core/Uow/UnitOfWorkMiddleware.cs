using Microsoft.AspNetCore.Http;
using NetMX.Ddd.Application.Uow;

namespace NetMX.AspNetCore.Core.Uow;

/// <summary>
/// Middleware that automatically wraps HTTP requests in a Unit of Work.
/// Commits the UoW if the request succeeds (status less than 400), rolls back on exceptions or errors.
/// </summary>
public class UnitOfWorkMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkMiddleware"/> class.
    /// </summary>
    public UnitOfWorkMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware, wrapping the request in a Unit of Work.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IUnitOfWorkManager uowManager)
    {
        // Skip if UoW already active (nested request)
        if (uowManager.Current != null)
        {
            await _next(context);
            return;
        }

        // Begin UoW for this request
        using var uow = uowManager.Begin();

        try
        {
            // Execute the request
            await _next(context);

            // If successful (status code < 400), commit
            if (context.Response.StatusCode < 400)
            {
                await uow.CompleteAsync();
            }
            // Otherwise UoW will rollback on dispose
        }
        catch
        {
            // Exception will rollback automatically on dispose
            throw;
        }
    }
}