using System.Net;
using Swap.Testing;
using Xunit;

namespace Swap.Htmx.TestApp.Tests;

public class TestAppTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;

    public TestAppTests(HtmxTestFixture<Program> fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Index_Returns_Htmx_Ready_View()
    {
        var response = await _client.HtmxGetAsync("/test");
        response.AssertSuccess();
        await response.AssertPartialViewAsync();
        await response.AssertElementExistsAsync("button[data-test-id='toast-success']");
    }

    [Fact]
    public async Task ToastSuccess_EmitsToast()
    {
        var response = await _client.HtmxPostAsync("/test/toast/success", formData: null);
        response.AssertSuccess();
        await response.AssertPartialViewAsync();
    }

    [Fact]
    public async Task CreateTodo_EmitsTodoEvents()
    {
        var form = new Dictionary<string, string>
        {
            ["title"] = "Buy milk"
        };

        var response = await _client.HtmxPostAsync("/test/todo/create", form);
        response.AssertSuccess();
        response.AssertHxTriggered("todo.created");
        response.AssertHxTriggered("ui.refreshList");
    }

    [Fact]
    public async Task SseStream_ExposesEventStream()
    {
        var response = await _client.GetAsync("/test/sse/stream");
        response.AssertStatus(HttpStatusCode.OK);
    }
}
