using Microsoft.Playwright;

namespace NetMX.Testing;

/// <summary>
/// Base class for Playwright E2E tests with HTMX-specific helpers.
/// Provides browser automation and HTMX pattern verification.
/// </summary>
public abstract class PlaywrightTestBase : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;

    /// <summary>
    /// Gets the current page instance.
    /// </summary>
    protected IPage Page => _page ?? throw new InvalidOperationException("Test not initialized. Call InitializeAsync first.");

    /// <summary>
    /// Gets the browser context.
    /// </summary>
    protected IBrowserContext Context => _context ?? throw new InvalidOperationException("Test not initialized. Call InitializeAsync first.");

    /// <summary>
    /// Initializes Playwright, browser, and page.
    /// Call this in your test setup (e.g., IAsyncLifetime.InitializeAsync).
    /// </summary>
    /// <param name="browserType">Browser to use (chromium, firefox, webkit). Default: chromium</param>
    /// <param name="headless">Run in headless mode. Default: true</param>
    public async Task InitializeAsync(string browserType = "chromium", bool headless = true)
    {
        _playwright = await Playwright.CreateAsync();
        
        _browser = browserType.ToLower() switch
        {
            "firefox" => await _playwright.Firefox.LaunchAsync(new() { Headless = headless }),
            "webkit" => await _playwright.Webkit.LaunchAsync(new() { Headless = headless }),
            _ => await _playwright.Chromium.LaunchAsync(new() { Headless = headless })
        };

        _context = await _browser.NewContextAsync();
        _page = await _context.NewPageAsync();
    }

    /// <summary>
    /// Disposes of Playwright resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_context != null) await _context.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    #region HTMX-Specific Helpers

    /// <summary>
    /// Waits for an HTMX request to complete.
    /// Monitors for htmx:beforeRequest and htmx:afterRequest events.
    /// </summary>
    /// <param name="url">URL to wait for (can be partial match)</param>
    /// <param name="timeout">Timeout in milliseconds. Default: 5000</param>
    public async Task WaitForHxRequestAsync(string url, int timeout = 5000)
    {
        // Wait for network request matching URL
        await Page.WaitForRequestAsync(
            request => request.Url.Contains(url),
            new() { Timeout = timeout }
        );

        // Wait for response
        await Page.WaitForResponseAsync(
            response => response.Url.Contains(url),
            new() { Timeout = timeout }
        );
    }

    /// <summary>
    /// Waits for an HTMX event to be triggered.
    /// </summary>
    /// <param name="eventName">HTMX event name (e.g., "htmx:load", "product-created")</param>
    /// <param name="timeout">Timeout in milliseconds. Default: 5000</param>
    public async Task WaitForHxEventAsync(string eventName, int timeout = 5000)
    {
        // Listen for custom event on document
        await Page.EvaluateAsync($@"
            new Promise((resolve) => {{
                const handler = () => {{
                    document.removeEventListener('{eventName}', handler);
                    resolve();
                }};
                document.addEventListener('{eventName}', handler);
                setTimeout(() => resolve(), {timeout});
            }})
        ");
    }

    /// <summary>
    /// Asserts that an HTMX request was made with the expected trigger header.
    /// </summary>
    /// <param name="eventName">Expected HX-Trigger header value</param>
    public async Task AssertHxTriggerAsync(string eventName)
    {
        // Intercept next response and check HX-Trigger header
        var response = await Page.RunAndWaitForResponseAsync(
            async () => await Task.Delay(100), // Wait for any pending request
            response => response.Headers.ContainsKey("HX-Trigger") || 
                       response.Headers.ContainsKey("hx-trigger")
        );

        var triggerHeader = response.Headers.GetValueOrDefault("HX-Trigger") ?? 
                           response.Headers.GetValueOrDefault("hx-trigger");

        if (triggerHeader == null || !triggerHeader.Contains(eventName))
        {
            throw new AssertionException($"Expected HX-Trigger header to contain '{eventName}', but got: {triggerHeader}");
        }
    }

    /// <summary>
    /// Clicks an element and waits for HTMX to swap content.
    /// </summary>
    /// <param name="selector">Element selector to click</param>
    /// <param name="waitForSelector">Selector to wait for after swap (optional)</param>
    public async Task ClickAndWaitForHxSwapAsync(string selector, string? waitForSelector = null)
    {
        // Click element
        await Page.ClickAsync(selector);

        // Wait for HTMX swap to complete
        await WaitForHxEventAsync("htmx:afterSwap");

        // Optionally wait for specific element
        if (!string.IsNullOrEmpty(waitForSelector))
        {
            await Page.WaitForSelectorAsync(waitForSelector);
        }
    }

    /// <summary>
    /// Asserts that an element was swapped by HTMX (has specific swap behavior).
    /// </summary>
    /// <param name="selector">Element selector</param>
    /// <param name="expectedSwap">Expected swap type (innerHTML, outerHTML, beforeend, etc.)</param>
    public async Task AssertHxSwapAsync(string selector, string expectedSwap)
    {
        var swapAttr = await Page.GetAttributeAsync(selector, "hx-swap");
        
        if (swapAttr == null || !swapAttr.Contains(expectedSwap))
        {
            throw new AssertionException($"Expected hx-swap='{expectedSwap}' on {selector}, but got: {swapAttr}");
        }
    }

    /// <summary>
    /// Fills a form and submits via HTMX.
    /// </summary>
    /// <param name="formSelector">Form selector</param>
    /// <param name="formData">Form field values (name → value)</param>
    public async Task FillAndSubmitHxFormAsync(string formSelector, Dictionary<string, string> formData)
    {
        // Fill form fields
        foreach (var (name, value) in formData)
        {
            await Page.FillAsync($"{formSelector} [name='{name}']", value);
        }

        // Submit form (triggers hx-post)
        var submitButton = await Page.QuerySelectorAsync($"{formSelector} button[type='submit']");
        if (submitButton != null)
        {
            await submitButton.ClickAsync();
        }
        else
        {
            // Try to find any button with hx-post
            await Page.ClickAsync($"{formSelector} [hx-post]");
        }

        // Wait for HTMX to complete
        await WaitForHxEventAsync("htmx:afterRequest");
    }

    /// <summary>
    /// Asserts that an element is currently loading via HTMX (has htmx-request class).
    /// </summary>
    /// <param name="selector">Element selector</param>
    public async Task AssertHxLoadingAsync(string selector)
    {
        var classes = await Page.GetAttributeAsync(selector, "class");
        
        if (classes == null || !classes.Contains("htmx-request"))
        {
            throw new AssertionException($"Expected element {selector} to have 'htmx-request' class, but got: {classes}");
        }
    }

    /// <summary>
    /// Waits for HTMX boosted navigation to complete.
    /// </summary>
    public async Task WaitForHxBoostAsync()
    {
        await WaitForHxEventAsync("htmx:pushedIntoHistory");
    }

    /// <summary>
    /// Gets the current URL after HTMX boost navigation.
    /// </summary>
    public string GetCurrentUrl() => Page.Url;

    /// <summary>
    /// Asserts that a delete confirmation was triggered.
    /// </summary>
    /// <param name="selector">Element with hx-confirm</param>
    public async Task AssertHxConfirmAsync(string selector)
    {
        var confirmAttr = await Page.GetAttributeAsync(selector, "hx-confirm");
        
        if (string.IsNullOrEmpty(confirmAttr))
        {
            throw new AssertionException($"Expected element {selector} to have 'hx-confirm' attribute");
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Takes a screenshot for debugging.
    /// </summary>
    /// <param name="path">Path to save screenshot</param>
    public async Task TakeScreenshotAsync(string path)
    {
        await Page.ScreenshotAsync(new() { Path = path });
    }

    /// <summary>
    /// Gets the inner text of an element.
    /// </summary>
    public async Task<string> GetTextAsync(string selector)
    {
        return await Page.InnerTextAsync(selector);
    }

    /// <summary>
    /// Asserts that an element contains specific text.
    /// </summary>
    public async Task AssertTextContainsAsync(string selector, string expectedText)
    {
        var text = await GetTextAsync(selector);
        if (!text.Contains(expectedText))
        {
            throw new AssertionException($"Expected element {selector} to contain '{expectedText}', but got: {text}");
        }
    }

    /// <summary>
    /// Asserts that an element is visible.
    /// </summary>
    public async Task AssertVisibleAsync(string selector)
    {
        var isVisible = await Page.IsVisibleAsync(selector);
        if (!isVisible)
        {
            throw new AssertionException($"Expected element {selector} to be visible");
        }
    }

    /// <summary>
    /// Asserts that an element is hidden.
    /// </summary>
    public async Task AssertHiddenAsync(string selector)
    {
        var isVisible = await Page.IsVisibleAsync(selector);
        if (isVisible)
        {
            throw new AssertionException($"Expected element {selector} to be hidden");
        }
    }

    #endregion
}

/// <summary>
/// Custom exception for assertion failures.
/// </summary>
public class AssertionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssertionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public AssertionException(string message) : base(message) { }
}
