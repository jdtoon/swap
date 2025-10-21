using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetMX.Events;

namespace NetMX.DependencyInjection;

/// <summary>
/// Extension methods for registering Event Bus services.
/// </summary>
public static class EventBusServiceCollectionExtensions
{
    /// <summary>
    /// Adds Event Bus services to the service collection.
    /// Uses IMemoryCache for in-process event handling (zero external dependencies).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        // Add memory cache if not already registered
        services.AddMemoryCache();

        // Register EventBus as singleton (shared across requests)
        services.TryAddSingleton<IEventBus, EventBus>();

        return services;
    }

    /// <summary>
    /// Registers an event handler for the specified event data type.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventHandler<THandler, TData>(this IServiceCollection services)
        where THandler : class, IEventHandler<TData>
    {
        services.AddScoped<IEventHandler<TData>, THandler>();
        return services;
    }
}
