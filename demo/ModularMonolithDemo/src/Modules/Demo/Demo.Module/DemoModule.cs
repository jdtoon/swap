using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using Swap.Modularity.Abstractions;
using ModularMonolithDemo.Modules.Todos.Contracts;

namespace ModularMonolithDemo.Modules.Demo.Module;

public sealed class DemoModule : IModule
{
    public string Name => "Demo";
    public IReadOnlyList<string> DependsOn => new[] { "Todos" };

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDemoQueries, DemoQueries>();
        services.AddScoped<IStatsService, StatsService>();
        services.AddSingleton<INotesService, NotesService>();
        services.AddScoped<IBulkService, BulkService>();
        services.AddSingleton<IComponentsService, ComponentsService>();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        // MVC controllers live in Demo.Web (RCL).
    }

    public void ConfigureEventChains(IEventChainRegistrar registrar)
    {
        // React to Todos domain events and update the activity log
        registrar.Register<object>(TodoEvents.Domain.Created, async (payload, sp) =>
        {
            var queries = sp.GetRequiredService<IDemoQueries>();
            queries.AppendActivity("Todo created");
            await Task.CompletedTask;
        });

        registrar.Register<object>(TodoEvents.Domain.Deleted, async (payload, sp) =>
        {
            var queries = sp.GetRequiredService<IDemoQueries>();
            queries.AppendActivity("Todo deleted");
            await Task.CompletedTask;
        });

        registrar.Register<object>(TodoEvents.Domain.Toggled, async (payload, sp) =>
        {
            var queries = sp.GetRequiredService<IDemoQueries>();
            queries.AppendActivity("Todo toggled");
            await Task.CompletedTask;
        });
    }
}

public interface IStatsService
{
    Task<StatsModel> GetStatsAsync();
}

public interface IDemoQueries
{
    Task<string> GetLatestMessageAsync();
    Task<IReadOnlyList<string>> GetActivityLogAsync();
    void AppendActivity(string message);
}

public interface INotesService
{
    Task<int> GetCountAsync();
    Task<IReadOnlyList<string>> GetAllAsync();
    Task AddAsync(string text);
}

public interface IBulkService
{
    Task<int> CompleteAsync(int[] ids);
}

public interface IComponentsService
{
    Task<int> GetCounterAsync(string name);
    Task<int> IncrementAsync(string name);
}

public record StatsModel(int Total, int Completed, int Pending);

internal sealed class StatsService : IStatsService
{
    private readonly ITodoService _todos;
    public StatsService(ITodoService todos) { _todos = todos; }
    public Task<StatsModel> GetStatsAsync()
    {
        var all = _todos.GetAll();
        var total = all.Count;
        var completed = all.Count(t => t.IsComplete);
        var pending = total - completed;
        return Task.FromResult(new StatsModel(total, completed, pending));
    }
}

internal sealed class DemoQueries : IDemoQueries
{
    private readonly LinkedList<string> _activity = new();
    private string _latest = "Ready";
    public Task<string> GetLatestMessageAsync() => Task.FromResult(_latest);
    public Task<IReadOnlyList<string>> GetActivityLogAsync() => Task.FromResult((IReadOnlyList<string>)_activity.ToList());
    public void AppendActivity(string message)
    {
        _latest = message;
        _activity.AddFirst($"{DateTime.Now:HH:mm:ss} - {message}");
        if (_activity.Count > 50) _activity.RemoveLast();
    }
}

internal sealed class NotesService : INotesService
{
    private readonly List<string> _notes = new();
    private readonly IDemoQueries _queries;
    public NotesService(IDemoQueries queries) { _queries = queries; }
    public Task<int> GetCountAsync() => Task.FromResult(_notes.Count);
    public Task<IReadOnlyList<string>> GetAllAsync() => Task.FromResult((IReadOnlyList<string>)_notes.ToList());
    public Task AddAsync(string text)
    {
        _notes.Add(text);
        _queries.AppendActivity($"Note added: {text}");
        return Task.CompletedTask;
    }
}

internal sealed class BulkService : IBulkService
{
    private readonly ITodoService _todos;
    private readonly IDemoQueries _queries;
    public BulkService(ITodoService todos, IDemoQueries queries) { _todos = todos; _queries = queries; }
    public Task<int> CompleteAsync(int[] ids)
    {
        if (ids == null || ids.Length == 0) return Task.FromResult(0);
        var all = _todos.GetAll();
        var set = new HashSet<int>(ids);
        int changed = 0;
        foreach (var item in all.Where(t => set.Contains(t.Id)))
        {
            if (!item.IsComplete)
            {
                _ = _todos.Toggle(item.Id);
                changed++;
            }
        }
        if (changed > 0) _queries.AppendActivity($"Completed {changed} todos");
        return Task.FromResult(changed);
    }
}

internal sealed class ComponentsService : IComponentsService
{
    private readonly Dictionary<string, int> _counters = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a"] = 0,
        ["b"] = 0
    };

    public Task<int> GetCounterAsync(string name)
    {
        name = (name ?? "a").ToLowerInvariant();
        if (!_counters.ContainsKey(name)) _counters[name] = 0;
        return Task.FromResult(_counters[name]);
    }

    public Task<int> IncrementAsync(string name)
    {
        name = (name ?? "a").ToLowerInvariant();
        if (!_counters.ContainsKey(name)) _counters[name] = 0;
        _counters[name]++;
        return Task.FromResult(_counters[name]);
    }
}
