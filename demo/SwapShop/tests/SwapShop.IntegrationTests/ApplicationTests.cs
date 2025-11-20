using Swap.Testing;
using SwapShop;
using System.Net;
using Xunit;

namespace SwapShop.IntegrationTests;

/// <summary>
/// Basic integration tests to verify the application starts and responds correctly
/// </summary>
public class ApplicationTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;

    public ApplicationTests(HtmxTestFixture<Program> fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Get_Homepage_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.AssertSuccess();
    }

    [Fact]
    public async Task Get_Products_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/Products");

        // Assert
        response.AssertSuccess();
    }

    [Fact]
    public async Task Get_Cart_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/Cart");

        // Assert
        response.AssertSuccess();
    }

    [Fact]
    public async Task Get_Orders_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/Orders");

        // Assert
        response.AssertSuccess();
    }
}
