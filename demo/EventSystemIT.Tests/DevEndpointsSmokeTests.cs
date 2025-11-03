using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EventSystemIT.Tests;

public class DevEndpointsSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DevEndpointsSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task Dashboard_Renders_And_Contains_Graph()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var resp = await client.GetAsync("/_swap/dev/events");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Swap Event Chains", html);
        Assert.Contains("class=\"mermaid\"", html);
    }

    [Fact]
    public async Task Meta_And_Explain_Work()
    {
        var client = _factory.CreateClient();
        var meta = await client.GetAsync("/_swap/dev/events.meta.json");
        Assert.Equal(HttpStatusCode.OK, meta.StatusCode);
        var explain = await client.GetAsync("/_swap/dev/explain.json?event=todo.created");
        Assert.Equal(HttpStatusCode.OK, explain.StatusCode);
    }
}
