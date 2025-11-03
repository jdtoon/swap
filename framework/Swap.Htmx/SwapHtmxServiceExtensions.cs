using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swap.Htmx.Events;
using Swap.Htmx.Middleware;
using Swap.Htmx.Dev;

namespace Swap.Htmx;

/// <summary>
/// Extension methods for registering Swap.Htmx services and middleware.
/// </summary>
public static class SwapHtmxServiceExtensions
{
    /// <summary>
    /// Adds Swap.Htmx services to the service collection.
    /// Currently this is a placeholder for future services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddSwapHtmx();
    /// </code>
    /// </example>
    public static IServiceCollection AddSwapHtmx(this IServiceCollection services)
    {
        // Core services required by Swap.Htmx
        services.AddHttpContextAccessor();

        // Default event bus + options (no chains by default)
        services.AddSingleton(new SwapEventBusOptions());
        services.AddScoped<ISwapEventBus, SwapEventBus>();
        return services;
    }

    /// <summary>
    /// Adds Swap.Htmx services and configures event chains.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureEvents">An optional configuration action to define event chains.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddSwapHtmx(this IServiceCollection services, Action<SwapEventBusOptions> configureEvents)
    {
        services.AddSwapHtmx();
        var opts = new SwapEventBusOptions();
        configureEvents?.Invoke(opts);
        // Guardrails: validate configuration early
        try
        {
            var diag = opts.Validate();
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isDev = string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
            if (diag.HasErrors && isDev)
            {
                var msg = "Swap.Htmx event chain validation failed:\n - " + string.Join("\n - ", diag.Errors);
                throw new InvalidOperationException(msg);
            }
            // In non-dev, we don't throw to avoid blocking startup; warnings/errors could be logged by host app.
        }
        catch
        {
            throw;
        }
        // Replace the default singleton with configured one
        services.AddSingleton(opts);
        return services;
    }

    /// <summary>
    /// Adds the Swap HTMX shell middleware to the application pipeline.
    /// This middleware enforces partial view responses for HTMX requests
    /// and helps debug issues where full pages are accidentally returned.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <example>
    /// <code>
    /// app.UseSwapHtmxShell();
    /// </code>
    /// </example>
    /// <remarks>
    /// Add this middleware early in the pipeline, typically right after
    /// UseRouting() and before endpoint middleware.
    /// </remarks>
    public static IApplicationBuilder UseSwapHtmxShell(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SwapHtmxShellMiddleware>();
    }

    /// <summary>
    /// Registers the Swap event middlewares. Must be added before MVC endpoints.
    /// This wires:
    /// - SwapEventContextMiddleware (reads X-Swap-Events and stores active subscriptions)
    /// - SwapEventResponseMiddleware (builds HX-Trigger from pending events at end of pipeline)
    /// </summary>
    public static IApplicationBuilder UseSwapHtmx(this IApplicationBuilder app)
    {
        return app
            .UseMiddleware<SwapEventContextMiddleware>()
            .UseMiddleware<SwapEventResponseMiddleware>();
    }
}
