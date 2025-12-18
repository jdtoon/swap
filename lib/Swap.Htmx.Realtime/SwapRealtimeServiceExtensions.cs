using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Swap.Htmx.Realtime;

namespace Swap.Htmx;

/// <summary>
/// Extension methods for registering Swap realtime services and middleware.
/// </summary>
public static class SwapRealtimeServiceExtensions
{
    /// <summary>
    /// Adds enhanced SSE services with connection management and event-driven broadcasting.
    /// Call this to enable advanced SSE features like rooms, authentication, and automatic event bridging.
    /// </summary>
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
    public static IServiceCollection AddSseFallback(this IServiceCollection services, Action<SseFallbackOptions>? configure = null)
    {
        var options = new SseFallbackOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton<ISseFallbackService, SseFallbackService>();

        return services;
    }

    /// <summary>
    /// Registers the SSE event middleware for automatic event-driven broadcasting.
    /// This should be called after UseSwapHtmx() to enable SSE event processing.
    /// </summary>
    public static IApplicationBuilder UseSseEventBridge(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RealtimeEventMiddleware>();
    }
}
