using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Swap.Modularity.Hosting;

public static class MvcBuilderExtensions
{
    /// <summary>
    /// Adds ApplicationParts for any loaded assemblies whose simple name ends with ".Web".
    /// This enables MVC discovery for module Razor Class Libraries without explicit host wiring.
    /// </summary>
    public static IMvcBuilder AddSwapModuleApplicationParts(this IMvcBuilder mvc)
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies();
        // Resolve the optional ISwapUiChainContributor interface via reflection to avoid hard reference to Swap.Htmx
        var contributorInterface = loaded
            .SelectMany(a => a.IsDynamic ? Array.Empty<Type>() : SafeGetTypes(a))
            .FirstOrDefault(t => t.IsInterface && t.FullName == "Swap.Htmx.Events.ISwapUiChainContributor");

        foreach (var a in loaded)
        {
            if (a.IsDynamic) continue;
            var name = a.GetName().Name ?? string.Empty;
            if (name.EndsWith(".Web", StringComparison.Ordinal))
            {
                // Avoid duplicate parts
                var already = mvc.PartManager.ApplicationParts
                    .OfType<AssemblyPart>()
                    .Any(p => p.Assembly == a);
                if (!already)
                {
                    mvc.PartManager.ApplicationParts.Add(new AssemblyPart(a));
                }

                // Also register any ISwapUiChainContributor implementations found in this assembly
                if (contributorInterface is not null)
                {
                    foreach (var t in SafeGetTypes(a).Where(t => contributorInterface.IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false }))
                    {
                        mvc.Services.AddSingleton(contributorInterface, t);
                    }
                }
            }
        }
        return mvc;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly a)
    {
        try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
    }
}
