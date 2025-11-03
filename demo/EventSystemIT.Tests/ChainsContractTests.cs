using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EventSystemIT.Tests;

public class ChainsContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ChainsContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task Chains_Json_Includes_TodoCreated_And_UiRefreshList()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/_swap/dev/events.json");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("todo.created", out var list), "todo.created chain not found");
        var found = list.EnumerateArray().Any(e => e.GetString() == "ui.todo.refreshList");
        Assert.True(found, "ui.todo.refreshList not found in todo.created chain");
    }
}
