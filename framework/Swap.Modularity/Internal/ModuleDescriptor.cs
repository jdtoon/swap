using Swap.Modularity.Abstractions;

namespace Swap.Modularity.Internal;

internal sealed record ModuleDescriptor(
    string Name,
    IReadOnlyList<string> DependsOn,
    IModule Instance);

internal sealed class ModuleCatalog
{
    public ModuleCatalog(IReadOnlyList<IModule> orderedModules)
    {
        OrderedModules = orderedModules;
    }

    public IReadOnlyList<IModule> OrderedModules { get; }
}
