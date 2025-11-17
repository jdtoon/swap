using Microsoft.AspNetCore.Mvc.Testing;
using SwapShop;
using System.Net;
using Xunit;

namespace SwapShop.IntegrationTests;

/// <summary>
/// Basic integration tests to verify the application starts and responds correctly
/// </summary>
public class ApplicationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApplicationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Homepage_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task Get_Products_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Products");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Get_Cart_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Cart");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Get_Orders_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Orders");

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
