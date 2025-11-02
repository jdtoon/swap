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
        freshClient.AsHtmxRequest().WithHeader("X-Swap-Events", "product.created,ui.refreshList,ui.showToast");

        var resp = await freshClient.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());
        resp.AssertSuccess();
        var names = resp.GetHxTriggerEventNames().ToArray();
        Assert.Contains("ui.refreshList", names);
        Assert.Contains("ui.showToast", names);
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

    [Fact]
    public async Task Multiple_Chained_Events_Are_Delivered_When_Subscribed()
    {
        // Arrange: listen to both UI events
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList,ui.showToast");

        // Act
        var resp = await _client.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());

        // Assert
        resp.AssertSuccess();
        resp.AssertHxTriggered("ui.refreshList");
        resp.AssertHxTriggered("ui.showToast");
    }

    [Fact]
    public async Task Duplicate_Emits_Last_Payload_Wins_For_Original_Event()
    {
        // Arrange: advertise original and chained to avoid filtering
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "product.created,ui.refreshList,ui.showToast");

        // Act
        var resp = await _client.HtmxPostAsync("/Products/CreateDuplicateEmits", new Dictionary<string, string>());

        // Assert: product.created present with id == 2 (last payload wins)
        resp.AssertSuccess();
        resp.AssertHxTriggered("product.created");
        using var json = resp.GetHxTriggerJson()!;
        var root = json.RootElement;
        Assert.True(root.TryGetProperty("product.created", out var created), "HX-Trigger missing 'product.created'.");
        Assert.True(created.TryGetProperty("id", out var idProp), "payload missing 'id'.");
        Assert.Equal(2, idProp.GetInt32());
    }

    [Fact]
    public async Task No_Header_Treated_As_No_Filter_And_Emits_Original_And_Chained()
    {
        // Arrange: use a fresh client with no headers
        var freshClient = new HtmxTestClient<EventSystemDemo.AppMarker>(_fixture.Factory);

        // Act
        var resp = await freshClient.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());

        // Assert: expect at least original and one chained event present
        resp.AssertSuccess();
        var names = resp.GetHxTriggerEventNames().ToArray();
        Assert.Contains("product.created", names);
        Assert.Contains("ui.refreshList", names);
    }

    [Fact]
    public async Task Empty_Header_Treated_As_No_Filter()
    {
        // Arrange: set an empty header value (whitespace)
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "   ");

        // Act
        var resp = await _client.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());

        // Assert
        resp.AssertSuccess();
        var names = resp.GetHxTriggerEventNames().ToArray();
        Assert.Contains("product.created", names);
        Assert.Contains("ui.refreshList", names);
    }

    [Fact]
    public async Task Unrelated_Subscriptions_Result_In_No_HxTrigger()
    {
        // Arrange: subscribe to an unrelated event name
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.unknown");

        // Act
        var resp = await _client.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());

        // Assert: no HX-Trigger header should be present
        var raw = resp.GetHxTriggerRaw();
        Assert.True(string.IsNullOrWhiteSpace(raw), $"Expected no HX-Trigger, but got: {raw}");
    }

    [Fact]
    public async Task Existing_Trigger_Key_Is_Overridden_By_Event_System_On_Collision()
    {
        // Arrange
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList");

        // Act
        var resp = await _client.HtmxPostAsync("/Products/EmitDirectUiEventCollision", new Dictionary<string, string>());

        // Assert: ui.refreshList payload should be from event system (beta), overriding pre-set alpha
        resp.AssertSuccess();
        resp.AssertHxTriggered("ui.refreshList");
        resp.AssertHxTriggerFieldEquals("ui.refreshList", "v", "beta");
    }

    [Fact]
    public async Task Filter_ShowToast_Only_Delivers_ShowToast()
    {
        // Arrange
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.showToast");

        // Act
        var resp = await _client.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());

        // Assert
        resp.AssertSuccess();
        var names = resp.GetHxTriggerEventNames().ToArray();
        Assert.Contains("ui.showToast", names);
        Assert.DoesNotContain("ui.refreshList", names);
        Assert.DoesNotContain("product.created", names);
    }

    [Fact]
    public async Task Extreme_Many_Emits_And_Subscriptions_All_Passed()
    {
        // Arrange: subscribe to 100 component events
        var events = Enumerable.Range(1, 100).Select(i => $"ui.component{i}");
        var header = string.Join(",", events);
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", header);

        // Act
        var resp = await _client.HtmxPostAsync("/Products/ExtremeEmit", new Dictionary<string, string>());

        // Assert
        resp.AssertSuccess();
        var names = resp.GetHxTriggerEventNames().ToArray();
        Assert.Equal(100, names.Length);
        Assert.Contains("ui.component1", names);
        Assert.Contains("ui.component100", names);
    }

    [Fact]
    public async Task Extreme_Unrelated_Subscriptions_No_Trigger()
    {
        // Arrange: subscribe to 100 unrelated names
        var unrelated = Enumerable.Range(1, 100).Select(i => $"ui.unrelated{i}");
        var header = string.Join(",", unrelated);
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", header);

        // Act
        var resp = await _client.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());

        // Assert
        var raw = resp.GetHxTriggerRaw();
        Assert.True(string.IsNullOrWhiteSpace(raw), $"Expected no HX-Trigger, but got: {raw}");
    }

    [Fact]
    public async Task Duplicate_UI_Emits_Last_Payload_Wins()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList");
        var resp = await _client.HtmxPostAsync("/Products/DuplicateUiEmits", new Dictionary<string, string>());
        resp.AssertSuccess();
        resp.AssertHxTriggered("ui.refreshList");
        resp.AssertHxTriggerFieldEquals("ui.refreshList", "v", "two");
    }

    [Fact]
    public async Task Malformed_Preexisting_HxTrigger_Does_Not_Break_And_Event_Is_Emitted()
    {
        // Arrange
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList");

        // Act
        var resp = await _client.HtmxPostAsync("/Products/MalformedPreTrigger", new Dictionary<string, string>());

        // Assert: Should still succeed and include ui.refreshList from event system
        resp.AssertSuccess();
        resp.AssertHxTriggered("ui.refreshList");
    resp.AssertHxTriggerFieldEquals("ui.refreshList", "status", "ok");
    }

    [Fact]
    public async Task No_Events_Result_In_No_HxTrigger()
    {
        _client.AsHtmxRequest();
        var resp = await _client.HtmxPostAsync("/Products/Noop", new Dictionary<string, string>());
        var raw = resp.GetHxTriggerRaw();
        Assert.True(string.IsNullOrWhiteSpace(raw), $"Expected no HX-Trigger, but got: {raw}");
    }

    [Fact]
    public async Task No_Events_With_Preexisting_Trigger_Is_Preserved()
    {
        _client.AsHtmxRequest();
        var resp = await _client.HtmxPostAsync("/Products/NoopWithPreTrigger", new Dictionary<string, string>());
        resp.AssertSuccess();
        using var json = resp.GetHxTriggerJson()!;
        var root = json.RootElement;
        Assert.True(root.TryGetProperty("preOnly", out var val), "Missing preOnly key");
        Assert.Equal("gamma", val.GetString());
    }

    [Fact]
    public async Task Duplicate_Subscriptions_Are_Deduped()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList,ui.refreshList,ui.showToast,ui.showToast");
        var resp = await _client.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());
        resp.AssertSuccess();
        var names = resp.GetHxTriggerEventNames().ToArray();
        Assert.Contains("ui.refreshList", names);
        Assert.Contains("ui.showToast", names);
        Assert.Equal(names.Length, names.Distinct().Count());
    }

    [Fact]
    public async Task Header_With_Whitespace_Is_Handled()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "  ui.refreshList ,  ui.showToast  ");
        var resp = await _client.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());
        resp.AssertSuccess();
        var names = resp.GetHxTriggerEventNames().ToArray();
        Assert.Contains("ui.refreshList", names);
        Assert.Contains("ui.showToast", names);
    }

    [Fact]
    public async Task Concurrency_Isolation_Across_Parallel_Requests()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            var c1 = new HtmxTestClient<EventSystemDemo.AppMarker>(_fixture.Factory);
            c1.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList");
            tasks.Add(Task.Run(async () =>
            {
                var r = await c1.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());
                r.AssertSuccess();
                Assert.Contains("ui.refreshList", r.GetHxTriggerEventNames());
            }));

            var c2 = new HtmxTestClient<EventSystemDemo.AppMarker>(_fixture.Factory);
            c2.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.unknown");
            tasks.Add(Task.Run(async () =>
            {
                var r = await c2.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());
                var raw = r.GetHxTriggerRaw();
                Assert.True(string.IsNullOrWhiteSpace(raw), $"Expected no HX-Trigger, but got: {raw}");
            }));
        }
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task Emit_On_BadRequest_Still_Emits_Header_Currently()
    {
        // Current behavior observation: middleware emits HX-Trigger even on 400 responses
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList");
        var resp = await _client.HtmxPostAsync("/Products/EmitThenBadRequest", new Dictionary<string, string>());

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        // HX-Trigger should still be present under current implementation
        resp.AssertHxTriggered("ui.refreshList");
        resp.AssertHxTriggerFieldEquals("ui.refreshList", "state", "bad");
    }

    [Fact]
    public async Task Emit_On_Redirect_Emits_Header_Currently()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList");
        var resp = await _client.HtmxPostAsync("/Products/EmitThenRedirect", new Dictionary<string, string>());

        // Using HX-Redirect so we can assert headers (avoids HttpClient auto-follow)
        resp.AssertHxRedirect("/Products/Noop");
        resp.AssertHxTriggered("ui.refreshList");
        resp.AssertHxTriggerFieldEquals("ui.refreshList", "state", "redirect");
    }

    [Fact]
    public async Task Subscription_Is_Case_Insensitive()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "UI.RefreshList");
        var resp = await _client.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());
        resp.AssertSuccess();
        resp.AssertHxTriggered("ui.refreshList");
    }
}
