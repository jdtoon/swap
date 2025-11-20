using Swap.Testing;

namespace SwapMinimal.Tests;

public class SwapMinimalTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;

    public SwapMinimalTests(HtmxTestFixture<Program> fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Get_Root_ReturnsHtml()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        await response
            .AssertSuccess()
            .AssertContainsAsync("SwapMinimal Demo");
    }

    [Fact]
    public async Task Get_Message_ReturnsPartial()
    {
        // Act
        var response = await _client.HtmxGetAsync("/message");

        // Assert
        await response.AssertSuccess().AssertContainsAsync("message-container");
        await response.AssertContainsAsync("Hello");
        await response.AssertContainsAsync("Rendered at");
    }
}
