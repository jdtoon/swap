using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace SwapMinimal.Tests;

public class SwapMinimalTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SwapMinimalTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Root_ReturnsHtml()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("SwapMinimal Demo", content);
    }

    [Fact]
    public async Task Get_Message_ReturnsPartial()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("HX-Request", "true");

        // Act
        var response = await client.GetAsync("/message");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("message-container", content);
        Assert.Contains("Hello", content);
        Assert.Contains("Rendered at", content);
    }
}
