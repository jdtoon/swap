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
            var c1 = new HtmxTestClient<MonolithDemo.AppMarker>(_fixture.Factory);
            c1.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList");
            tasks.Add(Task.Run(async () =>
            {
                var r = await c1.HtmxPostAsync("/Products/Create", new Dictionary<string, string>());
                r.AssertSuccess();
                Assert.Contains("ui.refreshList", r.GetHxTriggerEventNames());
            }));

            var c2 = new HtmxTestClient<MonolithDemo.AppMarker>(_fixture.Factory);
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
    public async Task Malformed_Preexisting_HxTrigger_Does_Not_Break_And_Event_Is_Emitted()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList");
        var resp = await _client.HtmxPostAsync("/Products/MalformedPreTrigger", new Dictionary<string, string>());
        resp.AssertSuccess();
        resp.AssertHxTriggered("ui.refreshList");
        resp.AssertHxTriggerFieldEquals("ui.refreshList", "status", "ok");
    }

    [Fact]
    public async Task Emit_On_BadRequest_Still_Emits_Header_Currently()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList");
        var resp = await _client.HtmxPostAsync("/Products/EmitThenBadRequest", new Dictionary<string, string>());
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
        resp.AssertHxTriggered("ui.refreshList");
        resp.AssertHxTriggerFieldEquals("ui.refreshList", "state", "bad");
    }

    [Fact]
    public async Task Emit_On_Redirect_Emits_Header_Currently()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList");
        var resp = await _client.HtmxPostAsync("/Products/EmitThenRedirect", new Dictionary<string, string>());
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

    [Fact]
    public async Task Emit_After_First_Write_Still_Emits_Header_Currently()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList");
        var resp = await _client.HtmxPostAsync("/Products/WriteThenEmit", new Dictionary<string, string>());
        resp.AssertHxTriggered("ui.refreshList");
        resp.AssertHxTriggerFieldEquals("ui.refreshList", "after", "write");
    }

    [Fact]
    public async Task Emit_Then_Throw_InternalServerError_Still_Emits_Header_Currently()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList");
        var resp = await _client.HtmxPostAsync("/Products/EmitThenThrow", new Dictionary<string, string>());
        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, resp.StatusCode);
        resp.AssertHxTriggered("ui.refreshList");
        resp.AssertHxTriggerFieldEquals("ui.refreshList", "state", "error");
    }

    [Fact]
    public async Task Nested_Collision_Last_Write_Wins_And_Replaces_Preexisting_Object()
    {
        _client.AsHtmxRequest().WithHeader("X-Swap-Events", "ui.refreshList");
        var resp = await _client.HtmxPostAsync("/Products/EmitNestedCollision", new Dictionary<string, string>());
        resp.AssertSuccess();
        using var json = resp.GetHxTriggerJson()!;
        var root = json.RootElement;
        Assert.True(root.TryGetProperty("ui.refreshList", out var ui), "Missing ui.refreshList");
        Assert.True(ui.TryGetProperty("v", out var v), "Missing v");
        Assert.Equal("beta", v.GetString());
        Assert.True(ui.TryGetProperty("nested", out var nested), "Missing nested");
        Assert.True(nested.TryGetProperty("x", out var x), "Missing nested.x");
        Assert.Equal(2, x.GetInt32());
        Assert.False(ui.TryGetProperty("keep", out _));
    }

    [Fact]
    public async Task Emits_For_Non_Htmx_Request_Currently()
    {
        var http = _fixture.Factory.CreateClient();
        var form = new System.Net.Http.FormUrlEncodedContent(new Dictionary<string, string>());
        var resp = await http.PostAsync("/Products/Create", form);
        Assert.True((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 400);
        Assert.True(resp.Headers.TryGetValues("HX-Trigger", out var values), "HX-Trigger header missing");
        var raw = values.FirstOrDefault();
        Assert.False(string.IsNullOrWhiteSpace(raw), "HX-Trigger value missing");
        using var doc = System.Text.Json.JsonDocument.Parse(raw!);
        Assert.True(doc.RootElement.TryGetProperty("ui.refreshList", out _), "ui.refreshList not found in HX-Trigger");
    }
}
