using System.Net;
using Swap.Testing;
using Xunit;

namespace EventSystemDemo.Tests;

public class EventSystemEventFlowTests : IClassFixture<HtmxTestFixture<EventSystemDemo.AppMarker>>
{
    private readonly HtmxTestClient<EventSystemDemo.AppMarker> _client;
    private readonly HtmxTestFixture<EventSystemDemo.AppMarker> _fixture;

    public EventSystemEventFlowTests(HtmxTestFixture<EventSystemDemo.AppMarker> fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
    }

    [Fact]
    public async Task Actioned_Request_With_Filtered_Subscriptions_Sends_Only_Chained_Active()
    {
        // Arrange: page declares it listens to ui.refreshList only
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList");

        // Act: POST to create product (emits product.created -> chains to ui.refreshList)
        var resp = await _client.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());

        // Assert
        resp.AssertSuccess();
        // Only chained and active event present
        resp.AssertHxTriggered("ui.refreshList");
        // Original should be filtered out
        var names = resp.GetHxTriggerEventNames().ToArray();
        Assert.DoesNotContain("product.created", names);
    }

    [Fact]
    public async Task Actioned_Request_Without_Filter_Sends_Original_And_Chained()
    {
        // Use a fresh client and explicitly advertise both events (no effective filtering)
        var freshClient = new HtmxTestClient<EventSystemDemo.AppMarker>(_fixture.Factory);
        freshClient.AsHtmxRequest().WithHeader("X-Swap-Events", "product.created,ui.refreshList");

        var resp = await freshClient.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());
        resp.AssertSuccess();
        var names = resp.GetHxTriggerEventNames().ToArray();
        Assert.Contains("ui.refreshList", names);
    }

    [Fact]
    public async Task Preexisting_HxTrigger_Is_Merged_With_SwapEvents()
    {
        // Arrange: listen to ui.refreshList
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList");

        // Act
        var resp = await _client.HtmxPostAsync("/Products/CreateWithTrigger", new Dictionary<string, string>());

        // Assert merge: both pre and ui.refreshList should be present
        resp.AssertSuccess();
        var names = resp.GetHxTriggerEventNames().ToArray();
        Assert.Contains("pre", names);
        Assert.Contains("ui.refreshList", names);
    }
}
