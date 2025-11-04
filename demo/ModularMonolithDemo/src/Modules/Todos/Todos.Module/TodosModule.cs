using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using Swap.Modularity.Abstractions;
using ModularMonolithDemo.Modules.Todos.Contracts;
using Swap.Htmx.Events;

namespace ModularMonolithDemo.Modules.Todos.Module;

public sealed class TodosModule : IModule
{
    public string Name => "Todos";
    public IReadOnlyList<string> DependsOn => Array.Empty<string>();

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ITodoService, InMemoryTodoService>();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        // MVC endpoints live in Todos.Web (RCL). Nothing to map here.
    }

    public void ConfigureEventChains(IEventChainRegistrar registrar)
    {
        // No server-side chains for Todos in this demo.
        // UI chains are declared via AddSwapHtmx in the host using the module-owned contributor.
    }
}

public interface ITodoService
{
    IReadOnlyList<TodoItemDto> GetAll();
    TodoItemDto Add(string title);
    TodoItemDto? Toggle(int id);
    bool Delete(int id);
}

internal sealed class InMemoryTodoService : ITodoService
{
    private readonly List<TodoItemDto> _items = new();
    private int _nextId = 1;

    public IReadOnlyList<TodoItemDto> GetAll() => _items.ToList();

    public TodoItemDto Add(string title)
    {
        var item = new TodoItemDto(_nextId++, title, false);
        _items.Add(item);
        return item;
    }

    public TodoItemDto? Toggle(int id)
    {
        var idx = _items.FindIndex(t => t.Id == id);
        if (idx < 0) return null;
        var cur = _items[idx];
        var updated = cur with { IsComplete = !cur.IsComplete };
        _items[idx] = updated;
        return updated;
    }

    public bool Delete(int id)
    {
        var removed = _items.RemoveAll(t => t.Id == id);
        return removed > 0;
    }
}
