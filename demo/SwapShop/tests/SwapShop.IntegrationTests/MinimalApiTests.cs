using Swap.Testing;
using SwapShop;
using Xunit;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SwapShop.IntegrationTests;

public class MinimalApiTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;

    public MinimalApiTests(HtmxTestFixture<Program> fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Post_Newsletter_ReturnsSuccess_WithToast_And_View()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "email", "test@example.com" }
        };
        var content = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.AsHtmxRequest().PostAsync("/api/newsletter", content);

        // Assert
        response.AssertSuccess();
        response.AssertHeader("HX-Trigger");
        
        await response.AssertContainsAsync("Thanks for subscribing!");
        await response.AssertContainsAsync("test@example.com");
    }

    [Fact]
    public async Task Post_Newsletter_InvalidEmail_ReturnsErrorToast()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "email", "invalid-email" }
        };
        var content = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.AsHtmxRequest().PostAsync("/api/newsletter", content);

        // Assert
        response.AssertSuccess();
        response.AssertHeader("HX-Trigger");
    }

    [Fact]
    public async Task Get_Status_ReturnsOperational()
    {
        // Act
        var response = await _client.AsHtmxRequest().GetAsync("/api/status");

        // Assert
        response.AssertSuccess();
        await response.AssertContainsAsync("System Operational");
    }
}
