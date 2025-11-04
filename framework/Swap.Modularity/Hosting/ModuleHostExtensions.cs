using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swap.Modularity.Abstractions;
using Swap.Modularity.Internal;

namespace Swap.Modularity.Hosting;

public static class ModuleHostExtensions
{
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

    public static IApplicationBuilder UseSwapModules(this IApplicationBuilder app)
    {
        // Endpoint routing happens via MapSwapModuleEndpoints in Program.cs
        return app;
    }

    public static IEndpointRouteBuilder MapSwapModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var catalog = endpoints.ServiceProvider.GetRequiredService<ModuleCatalog>();
        foreach (var module in catalog.OrderedModules)
        {
            module.ConfigureEndpoints(endpoints);
        }
        return endpoints;
    }

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
}
