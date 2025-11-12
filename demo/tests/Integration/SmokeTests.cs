using System.Threading.Tasks;
using System.Collections.Generic;
using Swap.Testing;
using Xunit;

namespace TaskFlow.IntegrationTests;

public class HtmxSmokeTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;

    public HtmxSmokeTests(HtmxTestFixture<Program> fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Todo_Htmx_Flow_Adds_And_Shows_Item()
    {
        // 1) Load the list as an HTMX partial
        var list = await _client.HtmxGetAsync("/Home/TodoList");
        list.AssertSuccess();
        await list.AssertPartialViewAsync();
        await list.AssertElementExistsAsync("#todo-list");

        // 2) Add a todo via HTMX
        var added = await _client.HtmxPostAsync("/Home/AddTodo", new Dictionary<string, string>
        {
            ["title"] = "HTMX integration test"
        });
        added.AssertSuccess();

        // 3) Reload list and assert the new item appears
        var listAfter = await _client.HtmxGetAsync("/Home/TodoList");
        listAfter.AssertSuccess();
        await listAfter.AssertPartialViewAsync();
        await listAfter.AssertContainsAsync("HTMX integration test");
    }
}
