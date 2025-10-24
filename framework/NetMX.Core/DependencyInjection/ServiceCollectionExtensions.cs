using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace NetMX.DependencyInjection;

/// <summary>
/// Extension methods for automatic service registration based on marker interfaces.
/// Scans assemblies for types implementing IScopedDependency, ITransientDependency, or ISingletonDependency
/// and automatically registers them with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Scans the specified assemblies and registers all services marked with dependency lifetime interfaces.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="assemblies">The assemblies to scan for services.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNetMXServices(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        if (assemblies == null || assemblies.Length == 0)
        {
            throw new ArgumentException("At least one assembly must be provided", nameof(assemblies));
        }

        foreach (var assembly in assemblies)
        {
            RegisterServicesFromAssembly(services, assembly);
        }

        return services;
    }

    /// <summary>
    /// Scans the assemblies containing the specified types and registers all services.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="types">Types whose assemblies will be scanned.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNetMXServicesFromTypes(
        this IServiceCollection services,
        params Type[] types)
    {
        if (types == null || types.Length == 0)
        {
            throw new ArgumentException("At least one type must be provided", nameof(types));
        }

        var assemblies = types.Select(t => t.Assembly).Distinct().ToArray();
        return AddNetMXServices(services, assemblies);
    }

    private static void RegisterServicesFromAssembly(IServiceCollection services, Assembly assembly)
    {
        // Get all concrete classes (not abstract, not interfaces)
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .ToList();

        // Register scoped services
        RegisterServicesByLifetime<IScopedDependency>(services, types, ServiceLifetime.Scoped);

        // Register transient services
        RegisterServicesByLifetime<ITransientDependency>(services, types, ServiceLifetime.Transient);

        // Register singleton services
        RegisterServicesByLifetime<ISingletonDependency>(services, types, ServiceLifetime.Singleton);
    }

    private static void RegisterServicesByLifetime<TMarker>(
        IServiceCollection services,
        List<Type> types,
        ServiceLifetime lifetime)
    {
        var markerType = typeof(TMarker);
        var servicesToRegister = types.Where(t => markerType.IsAssignableFrom(t)).ToList();

        foreach (var implementationType in servicesToRegister)
        {
            // Get all interfaces except the marker interface
            var interfaces = implementationType.GetInterfaces()
                .Where(i => i != markerType && !IsMarkerInterface(i))
                .ToList();

            if (interfaces.Count > 0)
            {
                // Register as each interface
                foreach (var interfaceType in interfaces)
                {
                    RegisterService(services, interfaceType, implementationType, lifetime);
                }
            }
            else
            {
                // No interfaces - register as self
                RegisterService(services, implementationType, implementationType, lifetime);
            }
        }
    }

    private static bool IsMarkerInterface(Type type)
    {
        return type == typeof(IScopedDependency) ||
               type == typeof(ITransientDependency) ||
               type == typeof(ISingletonDependency);
    }

    private static void RegisterService(
        IServiceCollection services,
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime)
    {
        // Check if already registered (avoid duplicates)
        if (services.Any(s => s.ServiceType == serviceType && s.ImplementationType == implementationType))
        {
            return;
        }

        var descriptor = new ServiceDescriptor(serviceType, implementationType, lifetime);
        services.Add(descriptor);
    }
}
