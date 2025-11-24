using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Swap.Htmx.Realtime;
using Swap.Htmx.Realtime.Redis;

namespace Swap.Htmx;

public static class SwapRedisExtensions
{
    /// <summary>
    /// Configures Swap to use Redis as the backplane for Server-Sent Events (SSE).
    /// This enables scaling SSE across multiple server instances.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration options for Redis connection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSwapRedisBackplane(this IServiceCollection services, Action<RedisSseOptions> configure)
    {
        services.Configure(configure);

        // Register IConnectionMultiplexer if not present, using the options
        // We use TryAddSingleton so we don't overwrite an existing Redis connection if the user already has one.
        // However, if the user wants to use a specific connection for Swap, they should configure it manually or we might need a named instance.
        // For now, we assume a shared Redis connection is fine or we create one if missing.
        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RedisSseOptions>>().Value;
            return ConnectionMultiplexer.Connect(options.Configuration);
        });

        // Replace the default InMemorySseBackplane with RedisSseBackplane
        services.Replace(ServiceDescriptor.Singleton<ISseBackplane, RedisSseBackplane>());

        return services;
    }
}
