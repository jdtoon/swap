using AngleSharp;
using AngleSharp.Html.Dom;
using System.Text.Json;
using System.Net;

namespace Swap.Testing;

/// <summary>
/// Wraps an <see cref="HttpResponseMessage"/> and exposes fluent assertion
/// helpers tailored for HTMX applications. This includes both general
/// HTTP/HTML checks and HTMX-specific header and attribute assertions.
/// </summary>
public class HtmxTestResponse
{
    private readonly HttpResponseMessage _response;
    private string? _cachedContent;
    private IHtmlDocument? _cachedDocument;

    public HtmxTestResponse(HttpResponseMessage response)
    {
        _response = response ?? throw new ArgumentNullException(nameof(response));
    }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public HttpStatusCode StatusCode => _response.StatusCode;

    /// <summary>
    /// Gets the response content as a string.
    /// </summary>
    public async Task<string> GetContentAsync()
    {
        if (_cachedContent == null)
        {
            _cachedContent = await _response.Content.ReadAsStringAsync();
        }
        return _cachedContent;
    }

    /// <summary>
    /// Gets the response as a parsed HTML document.
    /// </summary>
    public async Task<IHtmlDocument> GetDocumentAsync()
    {
        if (_cachedDocument == null)
        {
            var content = await GetContentAsync();
            var context = BrowsingContext.New(Configuration.Default);
            var document = await context.OpenAsync(req => req.Content(content));
            _cachedDocument = (IHtmlDocument)document;
        }
        return _cachedDocument;
    }

    /// <summary>
    /// Assert that the response has the expected status code.
    /// </summary>
    public HtmxTestResponse AssertStatus(HttpStatusCode expectedStatus)
    {
        if (_response.StatusCode != expectedStatus)
        {
            throw new HtmxTestException(
                $"Expected status code {(int)expectedStatus} ({expectedStatus}), but got {(int)_response.StatusCode} ({_response.StatusCode})");
        }
        return this;
    }

    /// <summary>
    /// Assert that the response is successful (2xx status code).
    /// </summary>
    public HtmxTestResponse AssertSuccess()
    {
        if (!_response.IsSuccessStatusCode)
        {
            throw new HtmxTestException(
                $"Expected successful status code (2xx), but got {(int)_response.StatusCode} ({_response.StatusCode})");
        }
        return this;
    }

    /// <summary>
    /// Assert that the response content contains the specified text.
    /// </summary>
    public async Task<HtmxTestResponse> AssertContainsAsync(string expectedText)
    {
        var content = await GetContentAsync();
        if (!content.Contains(expectedText))
        {
            throw new HtmxTestException(
                $"Expected response to contain '{expectedText}', but it was not found.\n\nActual content:\n{TruncateContent(content)}");
        }
        return this;
    }

    /// <summary>
    /// Assert that the response content does not contain the specified text.
    /// </summary>
    public async Task<HtmxTestResponse> AssertDoesNotContainAsync(string unexpectedText)
    {
        var content = await GetContentAsync();
        if (content.Contains(unexpectedText))
        {
            throw new HtmxTestException(
                $"Expected response to NOT contain '{unexpectedText}', but it was found.");
        }
        return this;
    }

    /// <summary>
    /// Assert that the response has a specific header.
    /// </summary>
    public HtmxTestResponse AssertHeader(string headerName, string? expectedValue = null)
    {
        if (!_response.Headers.TryGetValues(headerName, out var values))
        {
            throw new HtmxTestException($"Expected header '{headerName}' not found in response.");
        }

        if (expectedValue != null)
        {
            var actualValue = values.FirstOrDefault();
            if (actualValue != expectedValue)
            {
                throw new HtmxTestException(
                    $"Expected header '{headerName}' to have value '{expectedValue}', but got '{actualValue}'");
            }
        }

        return this;
    }

    /// <summary>
    /// Assert that the response contains an element matching the CSS selector.
    /// </summary>
    public async Task<HtmxTestResponse> AssertElementExistsAsync(string cssSelector)
    {
        var doc = await GetDocumentAsync();
        var element = doc.QuerySelector(cssSelector);
        
        if (element == null)
        {
            throw new HtmxTestException(
                $"Expected element matching selector '{cssSelector}' not found in response.");
        }
        
        return this;
    }

    /// <summary>
    /// Assert that an element does NOT exist for the selector.
    /// </summary>
    public async Task<HtmxTestResponse> AssertElementNotExistsAsync(string cssSelector)
    {
        var doc = await GetDocumentAsync();
        var element = doc.QuerySelector(cssSelector);
        if (element != null)
        {
            throw new HtmxTestException($"Did not expect element for selector '{cssSelector}' but one was found.");
        }
        return this;
    }

    /// <summary>
    /// Assert that the response contains an element with the specified HTMX attribute.
    /// </summary>
    public async Task<HtmxTestResponse> AssertHxAttributeAsync(string cssSelector, string attribute, string? expectedValue = null)
    {
        var doc = await GetDocumentAsync();
        var element = doc.QuerySelector(cssSelector);
        
        if (element == null)
        {
            throw new HtmxTestException(
                $"Element matching selector '{cssSelector}' not found.");
        }

        var actualValue = element.GetAttribute(attribute);
        
        if (actualValue == null)
        {
            throw new HtmxTestException(
                $"Element matching '{cssSelector}' does not have attribute '{attribute}'.");
        }

        if (expectedValue != null && actualValue != expectedValue)
        {
            throw new HtmxTestException(
                $"Expected attribute '{attribute}' to have value '{expectedValue}', but got '{actualValue}'");
        }

        return this;
    }

    /// <summary>
    /// Assert that the response contains an element with hx-get attribute.
    /// </summary>
    public async Task<HtmxTestResponse> AssertHxGetAsync(string cssSelector, string? expectedUrl = null)
    {
        return await AssertHxAttributeAsync(cssSelector, "hx-get", expectedUrl);
    }

    /// <summary>
    /// Assert that the response contains an element with hx-post attribute.
    /// </summary>
    public async Task<HtmxTestResponse> AssertHxPostAsync(string cssSelector, string? expectedUrl = null)
    {
        return await AssertHxAttributeAsync(cssSelector, "hx-post", expectedUrl);
    }

    /// <summary>
    /// Assert that the response contains an element with hx-target attribute.
    /// </summary>
    public async Task<HtmxTestResponse> AssertHxTargetAsync(string cssSelector, string? expectedTarget = null)
    {
        return await AssertHxAttributeAsync(cssSelector, "hx-target", expectedTarget);
    }

    /// <summary>
    /// Assert that the response contains an element with hx-swap attribute.
    /// </summary>
    public async Task<HtmxTestResponse> AssertHxSwapAsync(string cssSelector, string? expectedSwap = null)
    {
        return await AssertHxAttributeAsync(cssSelector, "hx-swap", expectedSwap);
    }

    /// <summary>
    /// Assert that the response contains an element with hx-trigger attribute.
    /// </summary>
    public async Task<HtmxTestResponse> AssertHxTriggerAsync(string cssSelector, string? expectedTrigger = null)
    {
        return await AssertHxAttributeAsync(cssSelector, "hx-trigger", expectedTrigger);
    }

    /// <summary>
    /// Assert that the response contains the expected number of elements matching the selector.
    /// </summary>
    public async Task<HtmxTestResponse> AssertElementCountAsync(string cssSelector, int expectedCount)
    {
        var doc = await GetDocumentAsync();
        var elements = doc.QuerySelectorAll(cssSelector);
        var actualCount = elements.Length;
        
        if (actualCount != expectedCount)
        {
            throw new HtmxTestException(
                $"Expected {expectedCount} element(s) matching '{cssSelector}', but found {actualCount}");
        }
        
        return this;
    }

    /// <summary>
    /// Assert that the response contains an element with specific text content.
    /// </summary>
    public async Task<HtmxTestResponse> AssertElementTextAsync(string cssSelector, string expectedText)
    {
        var doc = await GetDocumentAsync();
        var element = doc.QuerySelector(cssSelector);
        
        if (element == null)
        {
            throw new HtmxTestException(
                $"Element matching selector '{cssSelector}' not found.");
        }

        var actualText = element.TextContent.Trim();
        
        if (actualText != expectedText)
        {
            throw new HtmxTestException(
                $"Expected element '{cssSelector}' to have text '{expectedText}', but got '{actualText}'");
        }
        
        return this;
    }

    /// <summary>
    /// Assert that the element has a CSS class.
    /// </summary>
    public async Task<HtmxTestResponse> AssertHasCssClassAsync(string cssSelector, string className)
    {
        var doc = await GetDocumentAsync();
        var element = doc.QuerySelector(cssSelector);
        if (element == null)
            throw new HtmxTestException($"Element '{cssSelector}' not found.");

        var classes = element.ClassList;
        if (!classes.Contains(className))
        {
            throw new HtmxTestException($"Expected element '{cssSelector}' to have CSS class '{className}', but it was not present.");
        }

        return this;
    }

    /// <summary>
    /// Assert that the response contains an element with <c>hx-swap-oob</c>
    /// attribute.
    public async Task<HtmxTestResponse> AssertHxSwapOobAsync(string cssSelector, string? expectedValue = null)
    {
        return await AssertHxAttributeAsync(cssSelector, "hx-swap-oob", expectedValue);
    }

    /// <summary>
    /// Assert that the response represents a partial fragment rather than a
    /// full HTML document. This is done by scanning the raw content for
    /// <c>&lt;html&gt;</c>/<c>&lt;body&gt;</c> tags instead of relying on the parsed DOM.
    /// </summary>
    public async Task<HtmxTestResponse> AssertPartialViewAsync()
    {
        // Detect partial vs full by checking raw content for html/body tags. AngleSharp always creates a full
        // document tree (with <html>/<body>) even for fragments, so DOM-based checks would be misleading.
        var content = await GetContentAsync();
        var lowered = content.ToLowerInvariant();

        if (lowered.Contains("<html") || lowered.Contains("<body"))
        {
            throw new HtmxTestException(
                "Expected a partial view, but response contains full HTML structure (html/body tags).");
        }

        return this;
    }

    /// <summary>
    /// Assert that an anti-forgery token is present in a form (ASP.NET Core default).
    /// </summary>
    public async Task<HtmxTestResponse> AssertAntiForgeryTokenAsync(string formSelector = "form")
    {
        var doc = await GetDocumentAsync();
        var form = doc.QuerySelector(formSelector);
        if (form == null)
            throw new HtmxTestException($"Form '{formSelector}' not found.");

        var token = form.QuerySelector("input[name='__RequestVerificationToken']");
        if (token == null)
            throw new HtmxTestException("Anti-forgery token input '__RequestVerificationToken' not found in form.");

        return this;
    }

    /// <summary>
    /// Assert that the element contains an attribute whose value contains the expected substring.
    /// </summary>
    public async Task<HtmxTestResponse> AssertAttributeContainsAsync(string cssSelector, string attribute, string expectedSubstring)
    {
        var doc = await GetDocumentAsync();
        var element = doc.QuerySelector(cssSelector) ?? throw new HtmxTestException($"Element '{cssSelector}' not found.");

        var actualValue = element.GetAttribute(attribute);
        if (string.IsNullOrEmpty(actualValue) || !actualValue.Contains(expectedSubstring))
        {
            throw new HtmxTestException($"Expected attribute '{attribute}' on '{cssSelector}' to contain '{expectedSubstring}', but got '{actualValue}'.");
        }
        return this;
    }

    /// <summary>
    /// Execute a custom assertion on the HTML document.
    /// </summary>
    public async Task<HtmxTestResponse> AssertAsync(Func<IHtmlDocument, Task> assertion)
    {
        var doc = await GetDocumentAsync();
        await assertion(doc);
        return this;
    }

    /// <summary>
    /// Execute a custom assertion on the HTML document (synchronous).
    /// </summary>
    public async Task<HtmxTestResponse> AssertAsync(Action<IHtmlDocument> assertion)
    {
        var doc = await GetDocumentAsync();
        assertion(doc);
        return this;
    }

    // ---------------------
    // HTMX header assertions
    // ---------------------

    public HtmxTestResponse AssertHxRedirect(string expectedUrl)
        => AssertHeader("HX-Redirect", expectedUrl);

    public HtmxTestResponse AssertHxPushUrl(string? expectedValue = null)
        => AssertHeader("HX-Push-Url", expectedValue);

    public HtmxTestResponse AssertHxReswap(string? expectedValue = null)
        => AssertHeader("HX-Reswap", expectedValue);

    public HtmxTestResponse AssertHxRetarget(string? expectedValue = null)
        => AssertHeader("HX-Retarget", expectedValue);

    public HtmxTestResponse AssertHxRefresh(bool? expected = null)
    {
        if (expected == null)
            return AssertHeader("HX-Refresh");
        return AssertHeader("HX-Refresh", expected.Value ? "true" : "false");
    }

    public HtmxTestResponse AssertHxTriggerHeaderContains(string substring)
    {
        if (!_response.Headers.TryGetValues("HX-Trigger", out var values))
            throw new HtmxTestException("Expected header 'HX-Trigger' not found in response.");

        var actual = values.FirstOrDefault() ?? string.Empty;
        if (!actual.Contains(substring))
            throw new HtmxTestException($"Expected HX-Trigger header to contain '{substring}', but got '{actual}'.");
        return this;
    }

    public HtmxTestResponse AssertHxLocationContains(string substring)
    {
        if (!_response.Headers.TryGetValues("HX-Location", out var values))
            throw new HtmxTestException("Expected header 'HX-Location' not found in response.");

        var actual = values.FirstOrDefault() ?? string.Empty;
        if (!actual.Contains(substring))
            throw new HtmxTestException($"Expected HX-Location header to contain '{substring}', but got '{actual}'.");
        return this;
    }

    // Typed helpers around HX-Push-Url
    public HtmxTestResponse AssertHxPushUrlTrue() => AssertHxPushUrl("true");
    public HtmxTestResponse AssertHxPushUrlFalse() => AssertHxPushUrl("false");
    public HtmxTestResponse AssertHxPushUrlUrl(string expectedUrl) => AssertHxPushUrl(expectedUrl);

    public bool? GetHxPushUrlBool()
    {
        var v = GetHeaderValue("HX-Push-Url");
        if (v == null) return null;
        if (string.Equals(v, "true", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(v, "false", StringComparison.OrdinalIgnoreCase)) return false;
        return null; // it's a URL, not a boolean
    }

    // HX-Location JSON helpers
    public string? GetHxLocationRaw() => GetHeaderValue("HX-Location");

    public JsonDocument? GetHxLocationJson()
    {
        var raw = GetHxLocationRaw();
        if (string.IsNullOrWhiteSpace(raw)) return null;
        raw = raw.Trim();
        if (!raw.StartsWith("{")) return null; // not JSON
        try
        {
            return JsonDocument.Parse(raw);
        }
        catch (JsonException)
        {
            throw new HtmxTestException("HX-Location header is not valid JSON.");
        }
    }

    public HtmxTestResponse AssertHxLocationFieldEquals(string fieldName, string expected)
    {
        using var json = GetHxLocationJson() ?? throw new HtmxTestException("HX-Location JSON not present.");
        if (!json.RootElement.TryGetProperty(fieldName, out var prop))
            throw new HtmxTestException($"HX-Location JSON is missing field '{fieldName}'.");
        var actual = prop.GetString();
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            throw new HtmxTestException($"Expected HX-Location['{fieldName}'] == '{expected}', got '{actual}'.");
        return this;
    }

    public HtmxTestResponse AssertHxLocationFieldContains(string fieldName, string expectedSubstring)
    {
        using var json = GetHxLocationJson() ?? throw new HtmxTestException("HX-Location JSON not present.");
        if (!json.RootElement.TryGetProperty(fieldName, out var prop))
            throw new HtmxTestException($"HX-Location JSON is missing field '{fieldName}'.");
        var actual = prop.GetString() ?? string.Empty;
        if (!actual.Contains(expectedSubstring))
            throw new HtmxTestException($"Expected HX-Location['{fieldName}'] to contain '{expectedSubstring}', got '{actual}'.");
        return this;
    }

    // ---------------------
    // HX-Trigger typed helpers (HX-Trigger, HX-Trigger-After-Swap, HX-Trigger-After-Settle)
    // ---------------------

    /// <summary>
    /// Get the raw HX-Trigger header value (or other trigger header via <paramref name="headerName"/>).
    /// </summary>
    public string? GetHxTriggerRaw(string headerName = "HX-Trigger") => GetHeaderValue(headerName);

    /// <summary>
    /// If the trigger header contains JSON, returns it as a JsonDocument; otherwise null.
    /// </summary>
    public JsonDocument? GetHxTriggerJson(string headerName = "HX-Trigger")
    {
        var raw = GetHxTriggerRaw(headerName);
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var trimmed = raw.Trim();
        if (!trimmed.StartsWith("{")) return null;
        try
        {
            return JsonDocument.Parse(trimmed);
        }
        catch (JsonException)
        {
            throw new HtmxTestException($"{headerName} header is not valid JSON.");
        }
    }

    /// <summary>
    /// Returns the list of event names in the trigger header. Supports either JSON (keys) or plain string values
    /// using comma and/or whitespace separators.
    /// </summary>
    public IEnumerable<string> GetHxTriggerEventNames(string headerName = "HX-Trigger")
    {
        var raw = GetHxTriggerRaw(headerName);
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("{"))
        {
            using var json = GetHxTriggerJson(headerName);
            if (json == null) return Array.Empty<string>();
            return json.RootElement.EnumerateObject().Select(p => p.Name).ToArray();
        }

        // split by comma and whitespace
        var tokens = trimmed
            .Split(new[] { ',', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToArray();
        return tokens;
    }

    /// <summary>
    /// Assert that a specific event name is present in the trigger header.
    /// </summary>
    public HtmxTestResponse AssertHxTriggered(string eventName, string headerName = "HX-Trigger")
    {
        if (string.IsNullOrWhiteSpace(eventName)) throw new ArgumentException("Event name required", nameof(eventName));
        var names = GetHxTriggerEventNames(headerName).ToArray();
        if (!names.Contains(eventName))
        {
            var available = names.Length == 0 ? "<none>" : string.Join(", ", names);
            throw new HtmxTestException($"Expected {headerName} to include event '{eventName}', but available events were: {available}.");
        }
        return this;
    }

    /// <summary>
    /// Assert that an event payload field equals the expected value. Only valid for JSON trigger headers.
    /// </summary>
    public HtmxTestResponse AssertHxTriggerFieldEquals(string eventName, string fieldName, string expected, string headerName = "HX-Trigger")
    {
        using var json = GetHxTriggerJson(headerName) ?? throw new HtmxTestException($"{headerName} JSON not present.");
        if (!json.RootElement.TryGetProperty(eventName, out var ev))
            throw new HtmxTestException($"{headerName} JSON missing event '{eventName}'.");
        if (ev.ValueKind == JsonValueKind.Object)
        {
            if (!ev.TryGetProperty(fieldName, out var field))
                throw new HtmxTestException($"{headerName}['{eventName}'] missing field '{fieldName}'.");
            var actual = field.GetString();
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new HtmxTestException($"Expected {headerName}['{eventName}']['{fieldName}'] == '{expected}', got '{actual}'.");
        }
        else
        {
            throw new HtmxTestException($"{headerName}['{eventName}'] is not an object; cannot read field '{fieldName}'.");
        }
        return this;
    }

    /// <summary>
    /// Assert that an event payload field contains a substring. Only valid for JSON trigger headers.
    /// </summary>
    public HtmxTestResponse AssertHxTriggerFieldContains(string eventName, string fieldName, string expectedSubstring, string headerName = "HX-Trigger")
    {
        using var json = GetHxTriggerJson(headerName) ?? throw new HtmxTestException($"{headerName} JSON not present.");
        if (!json.RootElement.TryGetProperty(eventName, out var ev))
            throw new HtmxTestException($"{headerName} JSON missing event '{eventName}'.");
        if (ev.ValueKind == JsonValueKind.Object)
        {
            if (!ev.TryGetProperty(fieldName, out var field))
                throw new HtmxTestException($"{headerName}['{eventName}'] missing field '{fieldName}'.");
            var actual = field.GetString() ?? string.Empty;
            if (!actual.Contains(expectedSubstring))
                throw new HtmxTestException($"Expected {headerName}['{eventName}']['{fieldName}'] to contain '{expectedSubstring}', got '{actual}'.");
        }
        else
        {
            throw new HtmxTestException($"{headerName}['{eventName}'] is not an object; cannot read field '{fieldName}'.");
        }
        return this;
    }

    // Convenience wrappers for After-Swap and After-Settle
    public HtmxTestResponse AssertHxTriggeredAfterSwap(string eventName) => AssertHxTriggered(eventName, "HX-Trigger-After-Swap");
    public HtmxTestResponse AssertHxTriggeredAfterSettle(string eventName) => AssertHxTriggered(eventName, "HX-Trigger-After-Settle");
    public HtmxTestResponse AssertHxTriggerAfterSwapFieldEquals(string eventName, string fieldName, string expected)
        => AssertHxTriggerFieldEquals(eventName, fieldName, expected, "HX-Trigger-After-Swap");
    public HtmxTestResponse AssertHxTriggerAfterSettleFieldEquals(string eventName, string fieldName, string expected)
        => AssertHxTriggerFieldEquals(eventName, fieldName, expected, "HX-Trigger-After-Settle");
    public HtmxTestResponse AssertHxTriggerAfterSwapFieldContains(string eventName, string fieldName, string expectedSubstring)
        => AssertHxTriggerFieldContains(eventName, fieldName, expectedSubstring, "HX-Trigger-After-Swap");
    public HtmxTestResponse AssertHxTriggerAfterSettleFieldContains(string eventName, string fieldName, string expectedSubstring)
        => AssertHxTriggerFieldContains(eventName, fieldName, expectedSubstring, "HX-Trigger-After-Settle");

    /// <summary>
    /// Get a response header value (first value) or null if missing.
    /// </summary>
    public string? GetHeaderValue(string name)
    {
        return _response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
    }

    /// <summary>
    /// Get the HX-Redirect URL if present.
    /// </summary>
    public string? GetHxRedirectUrl() => GetHeaderValue("HX-Redirect");

    /// <summary>
    /// Assert that the response content matches a saved snapshot.
    /// </summary>
    /// <param name="snapshotName">Name of the snapshot (without extension)</param>
    /// <param name="snapshotDirectory">Directory to store snapshots (default: __snapshots__)</param>
    /// <param name="updateSnapshots">If true, update the snapshot instead of comparing</param>
    public async Task<HtmxTestResponse> AssertMatchesSnapshotAsync(
        string snapshotName,
        string? snapshotDirectory = null,
        bool? updateSnapshots = null)
    {
        var content = await GetContentAsync();
        var shouldUpdate = updateSnapshots ?? SnapshotManager.IsUpdateMode();
        
        await SnapshotManager.AssertMatchesSnapshotAsync(
            snapshotName,
            content,
            snapshotDirectory,
            shouldUpdate);

        return this;
    }

    private static string TruncateContent(string content, int maxLength = 500)
    {
        if (content.Length <= maxLength)
            return content;
        
        return content.Substring(0, maxLength) + "... (truncated)";
    }

    // ---------------------
    // Validation helpers
    // ---------------------

    /// <summary>
    /// Assert that the response contains any validation errors (summary or field-level).
    /// </summary>
    public async Task<HtmxTestResponse> AssertHasValidationErrorsAsync()
    {
        var doc = await GetDocumentAsync();
        // Common ASP.NET Core patterns: validation summary div with content, or spans with data-valmsg-for having text
        var summary = doc.QuerySelector(".validation-summary-errors, .alert.alert-error");
        if (summary != null && !string.IsNullOrWhiteSpace(summary.TextContent?.Trim()))
        {
            return this;
        }

        var anyField = doc.QuerySelectorAll("[data-valmsg-for], .field-validation-error, span.text-error")
            .Any(el => !string.IsNullOrWhiteSpace(el.TextContent?.Trim()));
        if (anyField)
            return this;

        throw new HtmxTestException("Expected validation errors, but none were found in summary or field messages.");
    }

    /// <summary>
    /// Assert that a specific field has a validation error. Looks for an element with data-valmsg-for="fieldName" and non-empty text.
    /// </summary>
    public async Task<HtmxTestResponse> AssertFieldValidationErrorAsync(string fieldName, string? messageContains = null)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            throw new ArgumentException("Field name is required", nameof(fieldName));

        var doc = await GetDocumentAsync();
        var el = doc.QuerySelector($"[data-valmsg-for='{fieldName}']");
        if (el == null)
            throw new HtmxTestException($"No validation message element found for field '{fieldName}'.");

        var text = (el.TextContent ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(text))
            throw new HtmxTestException($"Expected a validation message for field '{fieldName}', but it was empty.");

        if (messageContains != null && !text.Contains(messageContains, StringComparison.OrdinalIgnoreCase))
            throw new HtmxTestException($"Expected validation message for '{fieldName}' to contain '{messageContains}', but was '{text}'.");

        return this;
    }

    /// <summary>
    /// Assert that there is an out-of-band (hx-swap-oob) update for the element matching the selector, optionally verifying content contains a substring.
    /// </summary>
    public async Task<HtmxTestResponse> AssertOutOfBandAsync(string cssSelector, string? expectedContains = null)
    {
        await AssertHxSwapOobAsync(cssSelector);
        if (expectedContains != null)
        {
            var doc = await GetDocumentAsync();
            var el = doc.QuerySelector(cssSelector) ?? throw new HtmxTestException($"Element '{cssSelector}' not found for OOB assertion.");
            var content = el.InnerHtml ?? string.Empty;
            if (!content.Contains(expectedContains))
                throw new HtmxTestException($"Expected OOB content for '{cssSelector}' to contain '{expectedContains}'.");
        }
        return this;
    }

    /// <summary>
    /// Assert that the first top-level element in the partial has the expected id value, or that an element at the root level matches the id.
    /// </summary>
    public async Task<HtmxTestResponse> AssertPartialRootIdAsync(string expectedId)
    {
        if (string.IsNullOrWhiteSpace(expectedId)) throw new ArgumentException("Expected id required", nameof(expectedId));
        var doc = await GetDocumentAsync();
    var body = doc.Body ?? throw new HtmxTestException("Document body not available.");
    var rootChildren = body.Children;
        if (rootChildren.Length == 0)
            throw new HtmxTestException("Partial root assertion failed: no root elements found.");

        // Satisfy if any root child has the id
        foreach (var el in rootChildren)
        {
            var id = el.Id;
            if (!string.IsNullOrEmpty(id) && id.Equals(expectedId, StringComparison.Ordinal))
                return this;
        }

        throw new HtmxTestException($"Expected a root-level element with id '{expectedId}', but none was found.");
    }

    /// <summary>
    /// Assert that a root-level element matches the given selector (scope is document body immediate children).
    /// </summary>
    public async Task<HtmxTestResponse> AssertPartialRootMatchesAsync(string cssSelector)
    {
        var doc = await GetDocumentAsync();
    var body2 = doc.Body ?? throw new HtmxTestException("Document body not available.");
    var rootChildren = body2.Children;
        if (rootChildren.Length == 0)
            throw new HtmxTestException("Partial root assertion failed: no root elements found.");

        // Check if any root child matches the selector
        foreach (var el in rootChildren)
        {
            if (el.Matches(cssSelector))
                return this;
        }

        throw new HtmxTestException($"Expected a root-level element matching selector '{cssSelector}', but none matched.");
    }

    /// <summary>
    /// Assert that the response has no validation errors.
    /// </summary>
    public async Task<HtmxTestResponse> AssertNoValidationErrorsAsync()
    {
        var doc = await GetDocumentAsync();
        var summary = doc.QuerySelector(".validation-summary-errors, .alert.alert-error");
        var summaryHasText = summary != null && !string.IsNullOrWhiteSpace(summary.TextContent?.Trim());
        var anyField = doc.QuerySelectorAll("[data-valmsg-for], .field-validation-error, span.text-error")
            .Any(el => !string.IsNullOrWhiteSpace(el.TextContent?.Trim()));

        if (summaryHasText || anyField)
            throw new HtmxTestException("Expected no validation errors, but some were found.");

        return this;
    }
}
