using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectHub.Modules.Tasks.Contracts;
using ProjectHub.Modules.Tasks.Module.Persistence;
using ProjectHub.Modules.Tasks.Module.Services;
using Swap.Modularity.Abstractions;

namespace ProjectHub.Modules.Tasks.Module;

public class TasksModule : IModule
{
    public string Name => "Tasks";
    public IReadOnlyList<string> DependsOn => new[] { "Projects" };

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddTasksPersistence(configuration);
        services.AddScoped<ITaskService, EfTaskService>();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        // No custom endpoints needed - using controllers
    }
}
