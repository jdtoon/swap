using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using Swap.Modularity.Abstractions;
using ModularMonolithDemo.Modules.Todos.Contracts;
using Swap.Htmx.Events;
using ModularMonolithDemo.Modules.Todos.Module.Persistence;

namespace ModularMonolithDemo.Modules.Todos.Module;

public sealed class TodosModule : IModule
{
    public string Name => "Todos";
    public IReadOnlyList<string> DependsOn => Array.Empty<string>();

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register EF Core SQLite by default (module-owned), falls back to in-memory when disabled
        services.AddTodosPersistence(configuration);

    // Ensure MVC can discover controllers/views from the module's RCL automatically
        // by relying on host-side auto ApplicationPart discovery for *.Web assemblies.
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        // MVC endpoints live in Todos.Web (RCL). Nothing to map here.
    }

    public void ConfigureEventChains(IEventChainRegistrar registrar)
    {
        // No server-side chains for Todos in this demo.
        // UI chains are contributed via ISwapUiChainContributor from this module.
    }
}

public interface ITodoService
{
    IReadOnlyList<TodoItemDto> GetAll();
    TodoItemDto Add(string title);
    TodoItemDto? Toggle(int id);
    bool Delete(int id);
}
