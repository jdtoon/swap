using Microsoft.AspNetCore.Mvc.Razor;

namespace SwapModularMonolith.Infrastructure;

public class ModuleViewLocationExpander : IViewLocationExpander
{
    // Map controller names to module folders
    private static readonly Dictionary<string, ModuleViewConfig> ControllerModuleMap = new(StringComparer.OrdinalIgnoreCase)
    {
//#if (IncludeSampleModule)
        ["Notes"] = new("Notes"),
//#endif
        // Add mappings as modules are created:
        // ["ControllerName"] = new("ModuleFolderName"),
    };

    public void PopulateValues(ViewLocationExpanderContext context) { }

    public IEnumerable<string> ExpandViewLocations(
        ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations)
    {
        if (context.ControllerName != null &&
            ControllerModuleMap.TryGetValue(context.ControllerName, out var config))
        {
            var moduleLocations = new List<string>
            {
                $"/Modules/{config.ModuleName}/Views/{{0}}.cshtml",
                $"/Modules/{config.ModuleName}/Views/Shared/{{0}}.cshtml"
            };

            if (config.SubFolder != null)
            {
                moduleLocations.Insert(0, $"/Modules/{config.ModuleName}/Views/{config.SubFolder}/{{0}}.cshtml");
            }

            return moduleLocations.Concat(viewLocations);
        }

        return viewLocations;
    }

    private record ModuleViewConfig(string ModuleName, string? SubFolder = null);
}

public static class ModuleViewLocationExtensions
{
    public static void AddModuleViewLocations(this RazorViewEngineOptions options)
    {
        options.ViewLocationExpanders.Add(new ModuleViewLocationExpander());
    }
}
