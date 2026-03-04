using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Swap.Testing;

namespace Swap.Htmx.Tests;

/// <summary>
/// Unit tests for what's new in Swap.Testing v2:
///   - OOB swap introspection (GetOobSwapsAsync, AssertOobSwap*)
///   - Trigger payload assertions (GetTriggerPayload, AssertTriggerPayload, AssertTriggerCount)
///   - Form field helpers (AssertFormFieldExistsAsync, AssertFormValueAsync)
///   - Snapshot scrubber improvements (ScrubUrls, ScrubRegex)
/// </summary>
public class SwapTestingApiTests
{
    // ============================================================================
    // Helper: create HtmxTestResponse from raw HTML and optional headers
    // ============================================================================

    private static HtmxTestResponse CreateResponse(string html, HttpStatusCode status = HttpStatusCode.OK, Action<HttpResponseMessage>? configure = null)
    {
        var msg = new HttpResponseMessage(status)
        {
            Content = new StringContent(html, Encoding.UTF8, "text/html")
        };
        configure?.Invoke(msg);
        return new HtmxTestResponse(msg);
    }

    private static HtmxTestResponse CreateResponseWithTrigger(string triggerHeaderValue)
    {
        return CreateResponse("<div>ok</div>", configure: msg =>
        {
            msg.Headers.Add("HX-Trigger", triggerHeaderValue);
        });
    }

    // ============================================================================
    // OOB Swap Introspection
    // ============================================================================

    [Fact]
    public async Task GetOobSwapsAsync_ReturnsAllOobElements()
    {
        var html = """
            <div id="main">Hello</div>
            <div id="counter" hx-swap-oob="true">42</div>
            <div id="status" hx-swap-oob="innerHTML">Active</div>
            """;

        var response = CreateResponse(html);
        var swaps = await response.GetOobSwapsAsync();

        Assert.Equal(2, swaps.Count);
        Assert.Contains(swaps, s => s.TargetId == "counter" && s.SwapMode == "true" && s.HtmlContent == "42");
        Assert.Contains(swaps, s => s.TargetId == "status" && s.SwapMode == "innerHTML" && s.HtmlContent == "Active");
    }

    [Fact]
    public async Task GetOobSwapsAsync_ReturnsEmptyForNoOob()
    {
        var response = CreateResponse("<div>No OOB here</div>");
        var swaps = await response.GetOobSwapsAsync();
        Assert.Empty(swaps);
    }

    [Fact]
    public async Task AssertOobSwapExistsAsync_Passes_WhenTargetExists()
    {
        var html = """<div id="cart-count" hx-swap-oob="true">5</div>""";
        var response = CreateResponse(html);
        await response.AssertOobSwapExistsAsync("cart-count");
    }

    [Fact]
    public async Task AssertOobSwapExistsAsync_Throws_WhenTargetMissing()
    {
        var html = """<div id="other" hx-swap-oob="true">5</div>""";
        var response = CreateResponse(html);

        var ex = await Assert.ThrowsAsync<HtmxTestException>(
            () => response.AssertOobSwapExistsAsync("cart-count"));
        Assert.Contains("cart-count", ex.Message);
        Assert.Contains("other", ex.Message);
    }

    [Fact]
    public async Task AssertOobSwapContentAsync_Passes_WhenContentMatches()
    {
        var html = """<div id="notification" hx-swap-oob="true"><span>Item added</span></div>""";
        var response = CreateResponse(html);
        await response.AssertOobSwapContentAsync("notification", "Item added");
    }

    [Fact]
    public async Task AssertOobSwapContentAsync_Throws_WhenContentMismatch()
    {
        var html = """<div id="notification" hx-swap-oob="true"><span>Item added</span></div>""";
        var response = CreateResponse(html);

        var ex = await Assert.ThrowsAsync<HtmxTestException>(
            () => response.AssertOobSwapContentAsync("notification", "Item removed"));
        Assert.Contains("Item removed", ex.Message);
    }

    [Fact]
    public async Task AssertOobSwapContentAsync_Throws_WhenTargetMissing()
    {
        var response = CreateResponse("<div>No OOB</div>");

        var ex = await Assert.ThrowsAsync<HtmxTestException>(
            () => response.AssertOobSwapContentAsync("missing", "text"));
        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public async Task AssertOobSwapCountAsync_Passes_WithCorrectCount()
    {
        var html = """
            <div id="a" hx-swap-oob="true">1</div>
            <div id="b" hx-swap-oob="true">2</div>
            <div id="c" hx-swap-oob="innerHTML">3</div>
            """;
        var response = CreateResponse(html);
        await response.AssertOobSwapCountAsync(3);
    }

    [Fact]
    public async Task AssertOobSwapCountAsync_Throws_WithWrongCount()
    {
        var html = """<div id="a" hx-swap-oob="true">1</div>""";
        var response = CreateResponse(html);

        var ex = await Assert.ThrowsAsync<HtmxTestException>(
            () => response.AssertOobSwapCountAsync(2));
        Assert.Contains("2", ex.Message);
        Assert.Contains("1", ex.Message);
    }

    // ============================================================================
    // Trigger Payload Assertions
    // ============================================================================

    [Fact]
    public void GetTriggerPayload_DeserializesToType()
    {
        var response = CreateResponseWithTrigger("""{"showToast":{"message":"Created","type":"success"}}""");

        var payload = response.GetTriggerPayload<ToastPayload>("showToast");
        Assert.NotNull(payload);
        Assert.Equal("Created", payload!.Message);
        Assert.Equal("success", payload.Type);
    }

    [Fact]
    public void GetTriggerPayload_Throws_WhenEventMissing()
    {
        var response = CreateResponseWithTrigger("""{"showToast":{"message":"Hi"}}""");

        var ex = Assert.Throws<HtmxTestException>(
            () => response.GetTriggerPayload<ToastPayload>("missingEvent"));
        Assert.Contains("missingEvent", ex.Message);
    }

    [Fact]
    public void GetTriggerPayload_Throws_WhenNotJson()
    {
        var response = CreateResponseWithTrigger("simpleEvent");

        var ex = Assert.Throws<HtmxTestException>(
            () => response.GetTriggerPayload<ToastPayload>("simpleEvent"));
        Assert.Contains("not JSON", ex.Message);
    }

    [Fact]
    public void AssertTriggerPayload_Passes_WithSimplePath()
    {
        var response = CreateResponseWithTrigger("""{"showToast":{"message":"Created","type":"success"}}""");
        response.AssertTriggerPayload("showToast", "message", "Created");
    }

    [Fact]
    public void AssertTriggerPayload_Passes_WithDotPath()
    {
        var response = CreateResponseWithTrigger("""{"itemCreated":{"data":{"id":"123","name":"Test"}}}""");
        response.AssertTriggerPayload("itemCreated", "data.id", "123");
        response.AssertTriggerPayload("itemCreated", "data.name", "Test");
    }

    [Fact]
    public void AssertTriggerPayload_Throws_WhenValueMismatch()
    {
        var response = CreateResponseWithTrigger("""{"showToast":{"message":"Created"}}""");

        var ex = Assert.Throws<HtmxTestException>(
            () => response.AssertTriggerPayload("showToast", "message", "Deleted"));
        Assert.Contains("Deleted", ex.Message);
        Assert.Contains("Created", ex.Message);
    }

    [Fact]
    public void AssertTriggerPayload_Throws_WhenPathNotFound()
    {
        var response = CreateResponseWithTrigger("""{"showToast":{"message":"Hi"}}""");

        var ex = Assert.Throws<HtmxTestException>(
            () => response.AssertTriggerPayload("showToast", "nonexistent", "value"));
        Assert.Contains("nonexistent", ex.Message);
    }

    [Fact]
    public void AssertTriggerCount_Passes_WithCorrectCount()
    {
        var response = CreateResponseWithTrigger("""{"eventA":"1","eventB":"2","eventC":"3"}""");
        response.AssertTriggerCount(3);
    }

    [Fact]
    public void AssertTriggerCount_Passes_WithSimpleEvents()
    {
        var response = CreateResponseWithTrigger("eventA, eventB");
        response.AssertTriggerCount(2);
    }

    [Fact]
    public void AssertTriggerCount_Throws_WithWrongCount()
    {
        var response = CreateResponseWithTrigger("""{"eventA":"1","eventB":"2"}""");

        var ex = Assert.Throws<HtmxTestException>(
            () => response.AssertTriggerCount(3));
        Assert.Contains("3", ex.Message);
        Assert.Contains("2", ex.Message);
    }

    // ============================================================================
    // Form Field Helpers
    // ============================================================================

    [Fact]
    public async Task AssertFormFieldExistsAsync_Passes_WhenFieldExists()
    {
        var html = """
            <form>
                <input name="Email" type="email" />
                <select name="Role"><option>Admin</option></select>
                <textarea name="Notes"></textarea>
            </form>
            """;
        var response = CreateResponse(html);

        await response.AssertFormFieldExistsAsync("Email");
        await response.AssertFormFieldExistsAsync("Role");
        await response.AssertFormFieldExistsAsync("Notes");
    }

    [Fact]
    public async Task AssertFormFieldExistsAsync_Throws_WhenFieldMissing()
    {
        var html = """<form><input name="Name" /></form>""";
        var response = CreateResponse(html);

        var ex = await Assert.ThrowsAsync<HtmxTestException>(
            () => response.AssertFormFieldExistsAsync("Email"));
        Assert.Contains("Email", ex.Message);
    }

    [Fact]
    public async Task AssertFormFieldExistsAsync_Throws_WhenNoForm()
    {
        var response = CreateResponse("<div>No form</div>");

        var ex = await Assert.ThrowsAsync<HtmxTestException>(
            () => response.AssertFormFieldExistsAsync("Name"));
        Assert.Contains("form", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public async Task AssertFormValueAsync_Passes_ForInput()
    {
        var html = """<form><input name="Name" value="John" /></form>""";
        var response = CreateResponse(html);
        await response.AssertFormValueAsync("Name", "John");
    }

    [Fact]
    public async Task AssertFormValueAsync_Passes_ForCheckbox()
    {
        var html = """<form><input name="Active" type="checkbox" checked /></form>""";
        var response = CreateResponse(html);
        await response.AssertFormValueAsync("Active", "true");
    }

    [Fact]
    public async Task AssertFormValueAsync_Passes_ForSelect()
    {
        var html = """
            <form>
                <select name="Status">
                    <option value="pending">Pending</option>
                    <option value="active" selected>Active</option>
                </select>
            </form>
            """;
        var response = CreateResponse(html);
        await response.AssertFormValueAsync("Status", "active");
    }

    [Fact]
    public async Task AssertFormValueAsync_Passes_ForTextarea()
    {
        var html = """<form><textarea name="Bio">Hello World</textarea></form>""";
        var response = CreateResponse(html);
        await response.AssertFormValueAsync("Bio", "Hello World");
    }

    [Fact]
    public async Task AssertFormValueAsync_Throws_WhenValueMismatch()
    {
        var html = """<form><input name="Name" value="John" /></form>""";
        var response = CreateResponse(html);

        var ex = await Assert.ThrowsAsync<HtmxTestException>(
            () => response.AssertFormValueAsync("Name", "Jane"));
        Assert.Contains("Jane", ex.Message);
        Assert.Contains("John", ex.Message);
    }

    [Fact]
    public async Task AssertFormValueAsync_Throws_WhenFieldMissing()
    {
        var html = """<form><input name="Name" value="John" /></form>""";
        var response = CreateResponse(html);

        var ex = await Assert.ThrowsAsync<HtmxTestException>(
            () => response.AssertFormValueAsync("Email", "test@example.com"));
        Assert.Contains("Email", ex.Message);
    }

    // ============================================================================
    // Snapshot Scrubber Improvements
    // ============================================================================

    [Fact]
    public void ScrubUrls_ReplacesUrlsWithPlaceholder()
    {
        try
        {
            SnapshotManager.ClearScrubbers();
            SnapshotManager.UseDefaultScrubbers(false);
            SnapshotManager.ScrubUrls();

            var input = """<a href="https://example.com/page?q=1">Link</a> and http://localhost:5000/api""";
            // We test by using the scrubber pipeline via adding and running manually
            // Since ApplyScrubbers is private, test by round-tripping through MatchesSnapshotAsync
            // Instead, verify the AddScrubber path by checking scrubber works in isolation
            SnapshotManager.ClearScrubbers();
            SnapshotManager.ScrubUrls();

            // Use a delegate scrubber to verify the URL scrubber was added
            var scrubbed = ApplyScrubberViaSnapshot(input);
            Assert.Contains("[URL]", scrubbed);
            Assert.DoesNotContain("https://example.com", scrubbed);
            Assert.DoesNotContain("http://localhost", scrubbed);
        }
        finally
        {
            SnapshotManager.ClearScrubbers();
            SnapshotManager.UseDefaultScrubbers(true);
        }
    }

    [Fact]
    public void ScrubRegex_ReplacesMatchingPatternWithReplacement()
    {
        try
        {
            SnapshotManager.ClearScrubbers();
            SnapshotManager.UseDefaultScrubbers(false);
            SnapshotManager.ScrubRegex(@"tenant-\w+", "[TENANT]");

            var scrubbed = ApplyScrubberViaSnapshot("User from tenant-abc123 logged in");
            Assert.Contains("[TENANT]", scrubbed);
            Assert.DoesNotContain("tenant-abc123", scrubbed);
        }
        finally
        {
            SnapshotManager.ClearScrubbers();
            SnapshotManager.UseDefaultScrubbers(true);
        }
    }

    [Fact]
    public void ScrubRegex_Throws_WhenPatternEmpty()
    {
        Assert.Throws<ArgumentException>(() => SnapshotManager.ScrubRegex(""));
        Assert.Throws<ArgumentException>(() => SnapshotManager.ScrubRegex("  "));
    }

    // Helper: run scrubbers by doing a snapshot match round-trip
    private static string ApplyScrubberViaSnapshot(string input)
    {
        // SnapshotManager.ApplyScrubbers is private, but we can test the scrubber
        // pipeline by writing/reading a snapshot and checking the normalized output.
        // For unit tests, we replicate the scrubber pattern since we added them via the public API.
        var result = input;
        // Re-apply all registered scrubbers manually — the scrubbers are static so we can
        // verify they transform correctly by using the AddScrubber pipeline.
        // Actually, we can test via MatchesSnapshotAsync with update mode.
        var tempDir = Path.Combine(Path.GetTempPath(), $"swap_test_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);
            // Write in update mode to capture normalized output
            SnapshotManager.MatchesSnapshotAsync("test", input, tempDir, updateSnapshots: true).GetAwaiter().GetResult();
            // Read back the saved file
            return File.ReadAllText(Path.Combine(tempDir, "test.html"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    // ============================================================================
    // Test model for trigger payload deserialization
    // ============================================================================

    private sealed class ToastPayload
    {
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
