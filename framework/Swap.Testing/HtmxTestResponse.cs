using AngleSharp;
using AngleSharp.Html.Dom;
using System.Net;

namespace Swap.Testing;

/// <summary>
/// Represents an HTTP response with fluent assertion methods for HTMX testing.
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
    /// Assert that the response is a partial view (not a full page with html/body tags).

    /// <summary>
    /// Assert that the response contains an element with hx-swap-oob attribute.
    /// </summary>
    public async Task<HtmxTestResponse> AssertHxSwapOobAsync(string cssSelector, string? expectedValue = null)
    {
        return await AssertHxAttributeAsync(cssSelector, "hx-swap-oob", expectedValue);
    }
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
}
