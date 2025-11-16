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

        await response
            .AssertSuccess()
            .AssertPartialViewAsync()
            .AssertElementExistsAsync("button[data-test-id='toast-success']");
    }

    [Fact]
    public async Task ToastSuccess_EmitsToast()
    {
        var response = await _client.HtmxPostAsync("/test/toast/success", formData: null);

        await response
            .AssertSuccess()
            .AssertPartialViewAsync();
    }

    [Fact]
    public async Task CreateTodo_EmitsTodoEvents()
    {
        var form = new Dictionary<string, string>
        {
            ["title"] = "Buy milk"
        };

        var response = await _client.HtmxPostAsync("/test/todo/create", form);

        response
            .AssertSuccess()
            .AssertHxTriggered("todo.created")
            .AssertHxTriggered("ui.refreshList");
    }

    [Fact]
    public async Task SseStream_ExposesEventStream()
    {
        var response = await _client.GetAsync("/test/sse/stream");

        response.AssertStatus(HttpStatusCode.OK);
        var contentType = response.GetHeaderValue("Content-Type");
        Assert.NotNull(contentType);
        Assert.Contains("text/event-stream", contentType!);
    }
}
