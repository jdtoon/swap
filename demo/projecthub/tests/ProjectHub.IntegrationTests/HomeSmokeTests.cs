using System.Threading.Tasks;
using Swap.Testing;
using Xunit;

namespace ProjectHub.IntegrationTests;

public class HomeSmokeTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;

    public HomeSmokeTests(HtmxTestFixture<Program> fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Home_Index_Loads()
    {
        var resp = await _client.GetAsync("/");
        resp.AssertSuccess();
        await resp.AssertContainsAsync("Modular Monolith Demo");
        await resp.AssertElementExistsAsync(".navbar");
    }

    [Fact]
    public async Task Demo_Page_Loads()
    {
        var resp = await _client.GetAsync("/Demo");
        resp.AssertSuccess();
        await resp.AssertContainsAsync("Create Todo");
    }
}
