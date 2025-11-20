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
        response
            .AssertSuccess()
            .AssertToast("Note saved!", "success");
            
        await response.AssertHxSwapOobAsync("[hx-swap-oob]");
        await response.AssertContainsAsync(noteContent);
    }
}
