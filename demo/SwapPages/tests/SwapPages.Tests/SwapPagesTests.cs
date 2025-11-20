using Swap.Testing;

namespace SwapPages.Tests;

public class SwapPagesTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;

    public SwapPagesTests(HtmxTestFixture<Program> fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Get_Index_ReturnsHtml()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        await response
            .AssertSuccess()
            .AssertContainsAsync("Swap.Htmx Razor Pages Demo");
    }

    [Fact]
    public async Task Get_UpdateCounter_ReturnsPartial()
    {
        // Act
        // Use the new helper for Razor Page Handlers
        var response = await _client.HtmxGetPageHandlerAsync("/", "UpdateCounter", new { count = 0 });

        // Assert
        await response.AssertSuccess().AssertContainsAsync("Counter: 1");
        await response.AssertContainsAsync("Rendered at");
    }
}
