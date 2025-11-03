using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EventSystemIT.Tests;

public class ToastHeaderTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ToastHeaderTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task AddTodo_Returns_HX_Trigger_With_SuccessToast()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var content = new StringContent("title=FromTest", Encoding.UTF8, "application/x-www-form-urlencoded");
        var resp = await client.PostAsync("/Home/AddTodo", content);
        Assert.True(resp.StatusCode == HttpStatusCode.NoContent || resp.StatusCode == HttpStatusCode.OK);
        Assert.True(resp.Headers.TryGetValues("HX-Trigger", out var values));
        var header = string.Join(" ", values);
        Assert.Contains("ui.toast.success", header);
    }
}
