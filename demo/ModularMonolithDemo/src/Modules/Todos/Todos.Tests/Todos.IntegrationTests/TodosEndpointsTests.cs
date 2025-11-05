using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using ModularMonolithDemo.Modules.Todos.Contracts;
using Swap.Testing;
using Xunit;

namespace ModularMonolithDemo.Modules.Todos.IntegrationTests;

public class TodosEndpointsTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;
    private readonly HtmxTestFixture<Program> _fixture;

    public TodosEndpointsTests(HtmxTestFixture<Program> fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
    }

    [Fact]
    public async Task AddTodo_EmitsUiEvents_NoContent()
    {
        var resp = await _client.HtmxPostAsync("/todos/ui/add", new Dictionary<string,string>{{"title","Test item"}});
        resp.AssertStatus(System.Net.HttpStatusCode.NoContent);
        resp.AssertHxTriggered("ui.todo.refreshList");
        resp.AssertHxTriggered("ui.toast.success");
        resp.AssertHxTriggered("ui.stats.refresh");
        resp.AssertHxTriggered("ui.activity.append");
    }

    [Fact]
    public async Task ToggleTodo_ReturnsRowPartial_AndEmitsStatsRefresh()
    {
        // Seed a todo via DI
        using var scope = _fixture.Factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ITodoService>();
        var item = svc.Add("Seed");

        var resp = await _client.HtmxPostAsync($"/todos/ui/toggle/{item.Id}");
        resp.AssertSuccess();
        await resp.AssertPartialViewAsync();
        await resp.AssertElementExistsAsync($"#todo-item-{item.Id}");
        resp.AssertHxTriggered("ui.stats.refresh");
    }

    [Fact]
    public async Task DeleteTodo_NoContent_EmitsUiEvents()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ITodoService>();
        var item = svc.Add("ToDelete");

        var resp = await _client.HtmxDeleteAsync($"/todos/ui/delete/{item.Id}");
        resp.AssertStatus(System.Net.HttpStatusCode.NoContent);
        resp.AssertHxTriggered("ui.todo.refreshList");
        resp.AssertHxTriggered("ui.toast.success");
        resp.AssertHxTriggered("ui.stats.refresh");
        resp.AssertHxTriggered("ui.activity.append");
    }
}
