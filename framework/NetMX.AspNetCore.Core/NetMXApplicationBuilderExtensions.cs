using Microsoft.AspNetCore.Builder;
using NetMX.AspNetCore.Core.Exceptions;
using NetMX.AspNetCore.Core.Uow;
using NetMX.AspNetCore.Core.Validation;

namespace NetMX.AspNetCore.Core;

/// <summary>
/// Extension methods for configuring NetMX middleware in ASP.NET Core pipeline.
/// </summary>
public static class NetMXApplicationBuilderExtensions
{
    /// <summary>
    /// Adds NetMX exception handling middleware to the pipeline.
    /// Should be added early in the pipeline to catch all exceptions.
    /// </summary>
    public static IApplicationBuilder UseNetMXExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<NetMXExceptionMiddleware>();
    }

    /// <summary>
    /// Adds NetMX validation middleware to the pipeline.
    /// Catches ValidationException and returns 400 Bad Request with error details.
    /// </summary>
    public static IApplicationBuilder UseNetMXValidation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ValidationMiddleware>();
    }

    /// <summary>
    /// Adds NetMX Unit of Work middleware to the pipeline.
    /// Automatically wraps requests in a database transaction.
    /// </summary>
    public static IApplicationBuilder UseNetMXUnitOfWork(this IApplicationBuilder app)
    {
        return app.UseMiddleware<UnitOfWorkMiddleware>();
    }

    /// <summary>
    /// Adds all NetMX middleware to the pipeline in the correct order:
    /// 1. Exception handling
    /// 2. Validation handling
    /// 3. Unit of Work
    /// </summary>
    public static IApplicationBuilder UseNetMX(this IApplicationBuilder app)
    {
        return app
            .UseNetMXExceptionHandling()
            .UseNetMXValidation()
            .UseNetMXUnitOfWork();
    }
}