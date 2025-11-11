using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swap.Modularity.Abstractions;
using Swap.Modularity.Internal;

namespace Swap.Modularity.Hosting;

/// <summary>
/// Extension methods for registering and configuring Swap modules in the application host.
/// </summary>
public static class ModuleHostExtensions
{
    /// <summary>
    /// Registers all Swap modules and their services into the dependency injection container.
    /// Modules are discovered from the specified assemblies and loaded in dependency order.
    /// </summary>
    /// <param name="services">The service collection to add modules to.</param>
    /// <param name="configuration">Application configuration for passing to module initialization.</param>
    /// <param name="assemblies">Optional collection of assemblies to scan for modules. If null, scans all loaded assemblies.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSwapModules(this IServiceCollection services, IConfiguration configuration, IEnumerable<Assembly>? assemblies = null)
    {
        var catalog = BuildCatalog(configuration, assemblies);
        services.AddSingleton(catalog);
        foreach (var module in catalog.OrderedModules)
        {
            module.ConfigureServices(services, configuration);
        }
        return services;
    }

    /// <summary>
    /// Registers Swap module middleware into the application pipeline.
    /// Currently reserved for future middleware functionality.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseSwapModules(this IApplicationBuilder app)
    {
        // Endpoint routing happens via MapSwapModuleEndpoints in Program.cs
        return app;
    }

    /// <summary>
    /// Maps HTTP endpoints for all registered Swap modules.
    /// Call this after UseRouting() and before UseEndpoints() in your application startup.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapSwapModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var catalog = endpoints.ServiceProvider.GetRequiredService<ModuleCatalog>();
        foreach (var module in catalog.OrderedModules)
        {
            module.ConfigureEndpoints(endpoints);
        }
        return endpoints;
    }

    /// <summary>
    /// Configures event chains for all registered Swap modules.
    /// This connects domain events to UI events for each module.
    /// </summary>
    /// <param name="provider">The service provider for resolving module dependencies.</param>
    /// <param name="registrar">The event chain registrar for subscribing modules to events.</param>
    public static void ConfigureSwapModuleEventChains(this IServiceProvider provider, IEventChainRegistrar registrar)
    {
        var catalog = provider.GetRequiredService<ModuleCatalog>();
        foreach (var module in catalog.OrderedModules)
        {
            module.ConfigureEventChains(registrar);
        }
    }

    private static ModuleCatalog BuildCatalog(IConfiguration configuration, IEnumerable<Assembly>? assemblies)
    {
        // Proactively load referenced assemblies so modules referenced by the host are discoverable
        var entry = Assembly.GetEntryAssembly();
        if (entry is not null)
        {
            PreloadReferencedAssemblies(entry);
        }

        assemblies ??= AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).ToArray();
        var moduleTypes = assemblies
            .SelectMany(a => SafeGetTypes(a))
            .Where(t => typeof(IModule).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
            .ToArray();

        var modules = new List<IModule>(moduleTypes.Length);
        foreach (var type in moduleTypes)
        {
            if (Activator.CreateInstance(type) is IModule instance)
                modules.Add(instance);
        }

        var descriptors = modules.Select(m => new ModuleDescriptor(m.Name, m.DependsOn, m)).ToList();
        var ordered = TopologicalSort.Sort(descriptors, d => d.DependsOn, d => d.Name);
        return new ModuleCatalog(ordered.Select(d => d.Instance).ToList());
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
    }

    private static void PreloadReferencedAssemblies(Assembly root)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void LoadRecursive(Assembly asm)
        {
            foreach (var name in asm.GetReferencedAssemblies())
            {
                if (!visited.Add(name.FullName)) continue;
                try
                {
                    var loaded = Assembly.Load(name);
                    LoadRecursive(loaded);
                }
                catch
                {
                    // ignore load failures; continue best-effort
                }
            }
        }

        LoadRecursive(root);
    }
}
