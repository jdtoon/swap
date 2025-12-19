using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Swap.Htmx.Diagnostics;
using Swap.Htmx.Events;
using Swap.Htmx.Filters;
using Swap.Htmx.Middleware;
using Swap.Htmx.Dev;
using Swap.Htmx.Models;
using Swap.Htmx.Services;
using Swap.Htmx.State;

namespace Swap.Htmx;

/// <summary>
/// Extension methods for registering Swap.Htmx services and middleware.
/// </summary>
public static class SwapHtmxServiceExtensions
{
    /// <summary>
    /// Adds Swap.Htmx services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for Swap.Htmx features.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Minimal setup
    /// builder.Services.AddSwapHtmx();
    /// 
    /// // With configuration
    /// builder.Services.AddSwapHtmx(options => {
    ///     // Add view search paths for OOB partials
    ///     options.PartialViewSearchPaths.Add("Components");
    ///     
    ///     // Configure event chains
    ///     options.EventBus.When(MyEvents.Created)
    ///         .RefreshPartial("list", "_List")
    ///         .Toast("Created!", ToastType.Success);
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddSwapHtmx(this IServiceCollection services, Action<SwapHtmxOptions>? configure = null)
    {
        // Core services required by Swap.Htmx
        services.AddHttpContextAccessor();

        // Create and configure options
        var options = new SwapHtmxOptions();
        configure?.Invoke(options);
        
        // Auto-enable diagnostics in Development environment
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isDev = string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
        if (isDev)
        {
            // Enable dev-friendly defaults unless explicitly configured
            if (!options.Diagnostics.EnableClientLogging)
            {
                options.Diagnostics.EnableClientLogging = true;
            }
            options.Diagnostics.WarnOnUnhandledEvents = true;
            options.Diagnostics.WarnOnMissingOobTargets = true;
        }
        
        // Default assemblies to scan
        if (options.AssembliesToScan.Count == 0)
        {
            options.AssembliesToScan.Add(typeof(SwapHtmxOptions).Assembly); // Swap.Htmx
            var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                options.AssembliesToScan.Add(entryAssembly);
            }
        }
        
        // Apply decentralized configurations
        foreach (var configType in options.ConfigurationTypes)
        {
            var config = (ISwapEventConfiguration)Activator.CreateInstance(configType)!;
            config.Configure(options.EventBus);
        }
        
        // Validate event chains at startup if enabled
        if (options.Diagnostics.ValidateEventChainsOnStartup)
        {
            var diag = options.EventBus.Validate();
            if (diag.HasErrors && isDev)
            {
                var msg = "Swap.Htmx event chain validation failed:\n - " + string.Join("\n - ", diag.Errors);
                throw new InvalidOperationException(msg);
            }
        }
        
        // Register options singleton
        services.AddSingleton(options);
        
        // Register event bus options
        services.AddSingleton(options.EventBus);
        
        // Register event infrastructure
        services.AddScoped<ISwapEventBus, SwapEventBus>();
        services.AddScoped<IEventChainExecutor>(sp => new EventChainExecutor(options.EventBus));
        services.AddScoped<ISwapEventService, SwapEventService>();
        
        // Register view rendering service
        services.AddScoped<IViewRenderService, ViewRenderService>();
        
        // Register distributed handlers
        var registry = new SwapEventHandlerRegistry();
        registry.ScanAndRegisterHandlers(services, options.AssembliesToScan.ToArray());
        services.AddSingleton(registry);
        services.AddScoped<SwapEventHandlerExecutor>();
        
        // Register diagnostics
        if (isDev || options.Diagnostics.WarnOnUnhandledEvents || options.Diagnostics.WarnOnMissingOobTargets)
        {
            services.AddSingleton<ISwapDiagnostics, Diagnostics.SwapDiagnostics>();
        }
        else
        {
            services.AddSingleton<ISwapDiagnostics>(Diagnostics.NullSwapDiagnostics.Instance);
        }
        
        // Register user context (default to Session)
        services.TryAddScoped<ISwapUserContext, SessionSwapUserContext>();
        
        // Register SwapState model binder provider
        services.Configure<MvcOptions>(mvcOptions =>
        {
            mvcOptions.ModelBinderProviders.Insert(0, new SwapStateModelBinderProvider());
            
            // Register layout filter if auto-suppress is enabled
            if (options.AutoSuppressLayout)
            {
                mvcOptions.Filters.Add<SwapLayoutFilter>();
            }
        });
        
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
        services.AddHttpContextAccessor();
        
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
        
        // Register configured singleton
        services.AddSingleton(opts);
        services.AddScoped<ISwapEventBus, SwapEventBus>();
        
        // Event chain executor for HTTP responses
        services.AddScoped<IEventChainExecutor>(sp => new EventChainExecutor(opts));
        
        // Register SwapEventService
        services.AddScoped<ISwapEventService, Services.SwapEventService>();
        
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
    /// Registers the Swap event middleware. Must be added before MVC endpoints.
    /// This builds HX-Trigger headers from emitted events at the end of the request pipeline.
    /// </summary>
    public static IApplicationBuilder UseSwapHtmx(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SwapEventResponseMiddleware>();
    }

}
