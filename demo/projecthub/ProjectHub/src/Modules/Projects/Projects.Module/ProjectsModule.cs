using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using Swap.Modularity.Abstractions;
using ProjectHub.Modules.Projects.Contracts;
using Swap.Htmx.Events;
using ProjectHub.Modules.Projects.Module.Persistence;

namespace ProjectHub.Modules.Projects.Module;

public sealed class ProjectsModule : IModule
{
    public string Name => "Projects";
    public IReadOnlyList<string> DependsOn => new[] { "Workspaces" };

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddProjectsPersistence(configuration);
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        // MVC endpoints live in Projects.Web (RCL)
    }

    public void ConfigureEventChains(IEventChainRegistrar registrar)
    {
        // UI chains are contributed via ISwapUiChainContributor from Projects.Web
    }
}
