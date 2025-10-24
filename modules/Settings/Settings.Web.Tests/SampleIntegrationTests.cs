using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Settings.Web.Tests;

/// <summary>
/// Sample integration test using WebApplicationFactory.
/// Replace with actual HTTP integration tests.
/// </summary>
public class SampleIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SampleIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Sample_Integration_Test_Passes()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        var response = await client.GetAsync("/");
        
        // Assert
        response.EnsureSuccessStatusCode();
    }
}