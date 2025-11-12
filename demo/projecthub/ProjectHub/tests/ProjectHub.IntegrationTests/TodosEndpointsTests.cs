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
    public async Task Home_ReturnsSuccessAndHtml()
    {
        var response = await _client.GetAsync(\"/\");
        response.AssertSuccess();
        await response.AssertElementExistsAsync(\"h1\");
        await response.AssertContainsTextAsync(\"ProjectHub\");
    }
}
