using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using Swap.Modularity.Abstractions;
using ProjectHub.Modules.Workspaces.Contracts;
using Swap.Htmx.Events;
using ProjectHub.Modules.Workspaces.Module.Persistence;

namespace ProjectHub.Modules.Workspaces.Module;

public sealed class WorkspacesModule : IModule
{
    public string Name => "Workspaces";
    public IReadOnlyList<string> DependsOn => Array.Empty<string>();

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddWorkspacesPersistence(configuration);
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        // MVC endpoints live in Workspaces.Web (RCL)
    }

    public void ConfigureEventChains(IEventChainRegistrar registrar)
    {
        // UI chains are contributed via ISwapUiChainContributor from Workspaces.Web
    }
}
