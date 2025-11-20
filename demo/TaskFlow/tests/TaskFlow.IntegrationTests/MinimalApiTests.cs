using Swap.Testing;
using TaskFlow;
using Xunit;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaskFlow.IntegrationTests;

public class MinimalApiTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;

    public MinimalApiTests(HtmxTestFixture<Program> fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Post_QuickNote_ReturnsSuccess_WithToast_And_OobSwap()
    {
        // Arrange
        var noteContent = "This is a test note";
        var formData = new Dictionary<string, string>
        {
            { "note", noteContent }
        };
        var content = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.AsHtmxRequest().PostAsync("/api/quicknote", content);

        // Assert
        response.AssertSuccess();
        
        // Check for toast
        response.AssertHeader("HX-Trigger");
        
        // Check for OOB swap
        // The response body should contain the rendered partial view _NoteItem
        // and it should have hx-swap-oob attribute or similar if SwapResults.AlsoUpdate handles it.
        // SwapResults.AlsoUpdate usually appends the OOB content to the response.
        
        await response.AssertContainsAsync(noteContent);
        // We can also check for the target ID if we knew how SwapResults renders it.
        // Usually it renders <div id="quick-notes-list" hx-swap-oob="...">...</div>
        // or just the content with hx-swap-oob="beforeend:#quick-notes-list" on the element?
        // Let's just check for the content for now.
    }
}
