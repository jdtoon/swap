using Swap.Testing;
using Xunit;

namespace MonolithDemo.Tests;

public class EventSystemSmokeTests : IClassFixture<HtmxTestFixture<MonolithDemo.AppMarker>>
{
    private readonly HtmxTestClient<MonolithDemo.AppMarker> _client;
    private readonly HtmxTestFixture<MonolithDemo.AppMarker> _fixture;

    public EventSystemSmokeTests(HtmxTestFixture<MonolithDemo.AppMarker> fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
    }

    [Fact]
    public async Task Filtered_Subscriptions_Only_Chained_Active()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList");
        var resp = await _client.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());
        resp.AssertSuccess();
        resp.AssertHxTriggered("ui.refreshList");
        var names = resp.GetHxTriggerEventNames().ToArray();
        Assert.DoesNotContain("product.created", names);
    }

    [Fact]
    public async Task Merge_Preexisting_HxTrigger_With_SwapEvents()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList");
        var resp = await _client.HtmxPostAsync("/Products/CreateWithTrigger", new Dictionary<string, string>());
        resp.AssertSuccess();
        var names = resp.GetHxTriggerEventNames().ToArray();
        Assert.Contains("pre", names);
        Assert.Contains("ui.refreshList", names);
    }

    [Fact]
    public async Task Without_Filter_Original_And_Chained_Are_Emitted()
    {
        var freshClient = new HtmxTestClient<MonolithDemo.AppMarker>(_fixture.Factory);
        freshClient.AsHtmxRequest().WithHeader("X-Swap-Events", "product.created,ui.refreshList,ui.showToast");
        var resp = await freshClient.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());
        resp.AssertSuccess();
        var names = resp.GetHxTriggerEventNames().ToArray();
        Assert.Contains("ui.refreshList", names);
        Assert.Contains("ui.showToast", names);
    }

    [Fact]
    public async Task Multiple_Chained_Events_Delivered_When_Subscribed()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList,ui.showToast");
        var resp = await _client.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());
        resp.AssertSuccess();
        resp.AssertHxTriggered("ui.refreshList");
        resp.AssertHxTriggered("ui.showToast");
    }

    [Fact]
    public async Task Duplicate_Emits_Last_Payload_Wins()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "product.created,ui.refreshList,ui.showToast");
        var resp = await _client.HtmxPostAsync("/Products/CreateDuplicateEmits", new Dictionary<string, string>());
        resp.AssertSuccess();
        resp.AssertHxTriggered("product.created");
        using var json = resp.GetHxTriggerJson()!;
        var root = json.RootElement;
        Assert.True(root.TryGetProperty("product.created", out var created), "HX-Trigger missing 'product.created'.");
        Assert.True(created.TryGetProperty("id", out var idProp), "payload missing 'id'.");
        Assert.Equal(2, idProp.GetInt32());
    }

    [Fact]
    public async Task No_Header_Treated_As_No_Filter()
    {
        var freshClient = new HtmxTestClient<MonolithDemo.AppMarker>(_fixture.Factory);
        var resp = await freshClient.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());
        resp.AssertSuccess();
        var names = resp.GetHxTriggerEventNames().ToArray();
        Assert.Contains("product.created", names);
        Assert.Contains("ui.refreshList", names);
    }

    [Fact]
    public async Task Empty_Header_Treated_As_No_Filter()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "   ");
        var resp = await _client.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());
        resp.AssertSuccess();
        var names = resp.GetHxTriggerEventNames().ToArray();
        Assert.Contains("product.created", names);
        Assert.Contains("ui.refreshList", names);
    }

    [Fact]
    public async Task Unrelated_Subscriptions_Result_In_No_HxTrigger()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.unknown");
        var resp = await _client.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());
        var raw = resp.GetHxTriggerRaw();
        Assert.True(string.IsNullOrWhiteSpace(raw), $"Expected no HX-Trigger, but got: {raw}");
    }

    [Fact]
    public async Task Preexisting_Key_Overridden_By_Event_System_On_Collision()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList");
        var resp = await _client.HtmxPostAsync("/Products/EmitDirectUiEventCollision", new Dictionary<string, string>());
        resp.AssertSuccess();
        resp.AssertHxTriggered("ui.refreshList");
        resp.AssertHxTriggerFieldEquals("ui.refreshList", "v", "beta");
    }

    [Fact]
    public async Task Filter_ShowToast_Only_Delivers_ShowToast()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.showToast");
        var resp = await _client.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());
        resp.AssertSuccess();
        var names = resp.GetHxTriggerEventNames().ToArray();
        Assert.Contains("ui.showToast", names);
        Assert.DoesNotContain("ui.refreshList", names);
        Assert.DoesNotContain("product.created", names);
    }

    [Fact]
    public async Task Extreme_Unrelated_Subscriptions_No_Trigger()
    {
        var unrelated = Enumerable.Range(1, 100).Select(i => $"ui.unrelated{i}");
        var header = string.Join(",", unrelated);
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", header);
        var resp = await _client.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());
        var raw = resp.GetHxTriggerRaw();
        Assert.True(string.IsNullOrWhiteSpace(raw), $"Expected no HX-Trigger, but got: {raw}");
    }

    [Fact]
    public async Task Extreme_Subscriptions_And_Emits()
    {
        var events = Enumerable.Range(1, 100).Select(i => $"ui.component{i}");
        var header = string.Join(",", events);
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", header);
        var resp = await _client.HtmxPostAsync("/Products/ExtremeEmit", new Dictionary<string, string>());
        resp.AssertSuccess();
        var names = resp.GetHxTriggerEventNames().ToArray();
        Assert.Equal(100, names.Length);
        Assert.Contains("ui.component1", names);
        Assert.Contains("ui.component100", names);
    }
}
