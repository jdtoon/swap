//#if (IncludeSampleModule)
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace SwapModularMonolith.Tests;

public class NotesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public NotesControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Index_ReturnsSuccessStatusCode()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Notes");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Create_Get_ReturnsModal()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Notes/Create");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("New Note", content);
    }

    [Fact]
    public async Task Create_Post_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Title", "Test Note"),
            new KeyValuePair<string, string>("Content", "Test content"),
            new KeyValuePair<string, string>("Color", "blue")
        });

        // Act
        var response = await client.PostAsync("/Notes/Create", formData);

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
//#endif