using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace SwapPages.Tests;

public class SwapPagesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SwapPagesTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Index_ReturnsHtml()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Swap.Htmx Razor Pages Demo", content);
    }

    [Fact]
    public async Task Get_UpdateCounter_ReturnsPartial()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("HX-Request", "true");

        // Act
        var response = await client.GetAsync("/?handler=UpdateCounter&count=0");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Counter: 1", content);
        Assert.Contains("Rendered at", content);
    }
}
