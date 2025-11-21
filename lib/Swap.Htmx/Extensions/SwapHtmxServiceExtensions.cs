using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Swap.Htmx.Events;
using Swap.Htmx.Middleware;
using Swap.Htmx.Dev;
using Swap.Htmx.Realtime;
using Swap.Htmx.Models;
using Swap.Htmx.Services;

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
        
        // Apply decentralized configurations
        foreach (var configType in options.ConfigurationTypes)
        {
            var config = (ISwapEventConfiguration)Activator.CreateInstance(configType)!;
            config.Configure(options.EventBus);
        }
        
        // Register options singleton
        services.AddSingleton(options);
        
        // Register event bus options
        services.AddSingleton(options.EventBus);
        
        // Register event infrastructure
        services.AddScoped<ISwapEventBus, SwapEventBus>();
        services.AddScoped<IEventChainExecutor>(sp => new EventChainExecutor(options.EventBus));
        services.AddScoped<ISwapEventService, SwapEventService>();
        
        // Register user context (default to Session)
        services.TryAddScoped<ISwapUserContext, SessionSwapUserContext>();
        
        return services;
    }

    /// <summary>
    /// Adds enhanced SSE services with connection management and event-driven broadcasting.
    /// Call this to enable advanced SSE features like rooms, authentication, and automatic event bridging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddSwapHtmx()
    ///                 .AddSseEventBridge();
    /// </code>
    /// </example>
    public static IServiceCollection AddSseEventBridge(this IServiceCollection services)
    {
        services.TryAddSingleton<ISseBackplane, InMemorySseBackplane>();
        
        // Register Registry (implements both ISseConnectionRegistry and IRealtimeConnectionRegistry)
        services.AddSingleton<SseConnectionRegistry>();
        services.AddSingleton<ISseConnectionRegistry>(sp => sp.GetRequiredService<SseConnectionRegistry>());
        services.AddSingleton<IRealtimeConnectionRegistry>(sp => sp.GetRequiredService<SseConnectionRegistry>());

        // Register Bridge (implements both ISseEventBridge and IRealtimeEventBridge)
        services.AddScoped<RealtimeEventBridge>();
        services.AddScoped<ISseEventBridge>(sp => sp.GetRequiredService<RealtimeEventBridge>());
        services.AddScoped<IRealtimeEventBridge>(sp => sp.GetRequiredService<RealtimeEventBridge>());

        services.AddScoped<IRealtimeInputHandler, DefaultRealtimeInputHandler>();

        services.AddScoped<ISseViewRenderer, SseViewRenderer>();
        services.AddHttpContextAccessor(); // Required for view rendering
        return services;
    }

    /// <summary>
    /// Adds SSE fallback services for polling support when SSE connections fail.
    /// This enables graceful degradation to HTTP polling for unreliable networks.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for fallback behavior.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddSseEventBridge()
    ///                 .AddSseFallback(options => {
    ///                     options.DefaultPollInterval = 3000;
    ///                     options.MaxSseRetries = 5;
    ///                 });
    /// </code>
    /// </example>
    public static IServiceCollection AddSseFallback(this IServiceCollection services, Action<SseFallbackOptions>? configure = null)
    {
        var options = new SseFallbackOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton<ISseFallbackService, SseFallbackService>();

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

    /// <summary>
    /// Registers the SSE event middleware for automatic event-driven broadcasting.
    /// This should be called after UseSwapHtmx() to enable SSE event processing.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <example>
    /// <code>
    /// app.UseSwapHtmx()
    ///    .UseSseEventBridge();
    /// </code>
    /// </example>
    public static IApplicationBuilder UseSseEventBridge(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RealtimeEventMiddleware>();
    }
}
