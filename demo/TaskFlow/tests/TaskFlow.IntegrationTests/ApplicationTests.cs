using Swap.Testing;
using TaskFlow;
using Xunit;
using System.Threading.Tasks;

namespace TaskFlow.IntegrationTests;

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
}
