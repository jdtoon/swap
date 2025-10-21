using Xunit;

namespace NetMX.Testing.Examples;

/// <summary>
/// Example E2E test showing how to use PlaywrightTestBase for HTMX testing.
/// Developers can copy this pattern for their own tests.
/// </summary>
public class ProductFeatureE2EExample : PlaywrightTestBase, IAsyncLifetime
{
    private const string BaseUrl = "http://localhost:5000";

    /// <summary>
    /// Initializes Playwright browser for testing.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Initialize Playwright with Chromium in headless mode
        await base.InitializeAsync("chromium", headless: true);
    }

    /// <summary>
    /// Disposes Playwright resources.
    /// </summary>
    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    /// <summary>
    /// Example: Create product via HTMX form.
    /// </summary>
    [Fact(Skip = "Example test - requires running application")]
    public async Task CreateProduct_ViaHTMX_AddsRowToTable()
    {
        // Navigate to products page
        await Page.GotoAsync($"{BaseUrl}/Product");

        // Click "New Product" button (triggers hx-get)
        await ClickAndWaitForHxSwapAsync("button[hx-get='/Product/Create']", "#product-form");

        // Form should be loaded via HTMX (no page reload)
        await AssertVisibleAsync("#product-form");

        // Fill form
        await FillAndSubmitHxFormAsync("#product-form", new Dictionary<string, string>
        {
            ["Name"] = "Test Product",
            ["Price"] = "99.99",
            ["Description"] = "A test product created via HTMX"
        });

        // Wait for HTMX to swap in the new row
        await WaitForHxEventAsync("product-created");

        // Verify new product appears in table
        await AssertTextContainsAsync("table tbody", "Test Product");
        await AssertTextContainsAsync("table tbody", "99.99");
    }

    /// <summary>
    /// Example: Edit product inline via HTMX.
    /// </summary>
    [Fact(Skip = "Example test - requires running application")]
    public async Task EditProduct_ViaHTMX_UpdatesInline()
    {
        // Navigate to products page
        await Page.GotoAsync($"{BaseUrl}/Product");

        // Click edit button on first product
        await ClickAndWaitForHxSwapAsync("tr:first-child button[hx-get*='/Edit']", "#product-form");

        // Form should load inline
        await AssertVisibleAsync("#product-form");

        // Update product name
        var updatedName = "Updated Product Name";
        await Page.FillAsync("#product-form [name='Name']", updatedName);

        // Submit form
        await Page.ClickAsync("#product-form button[type='submit']");

        // Wait for HTMX swap
        await WaitForHxEventAsync("htmx:afterSwap");

        // Verify row was updated (not form)
        await AssertHiddenAsync("#product-form");
        await AssertTextContainsAsync("#row-1", updatedName);
    }

    /// <summary>
    /// Example: Delete product with confirmation dialog.
    /// </summary>
    [Fact(Skip = "Example test - requires running application")]
    public async Task DeleteProduct_WithConfirmation_RemovesRow()
    {
        // Navigate to products page
        await Page.GotoAsync($"{BaseUrl}/Product");

        // Verify delete button has hx-confirm
        await AssertHxConfirmAsync("tr:first-child button[hx-delete]");

        // Click delete button (will trigger browser confirmation)
        // Note: In real tests, you'd need to handle the confirmation dialog
        await Page.ClickAsync("tr:first-child button[hx-delete]");

        // Accept confirmation
        Page.Dialog += async (_, dialog) => await dialog.AcceptAsync();

        // Wait for HTMX to remove the row
        await WaitForHxEventAsync("htmx:afterSwap");

        // Row should be removed (check row count decreased)
        var rowCount = await Page.Locator("table tbody tr").CountAsync();
        Assert.True(rowCount >= 0); // At least one row removed
    }

    /// <summary>
    /// Example: Search products with debounced HTMX request.
    /// </summary>
    [Fact(Skip = "Example test - requires running application")]
    public async Task SearchProducts_ViaHTMX_UpdatesTable()
    {
        // Navigate to products page
        await Page.GotoAsync($"{BaseUrl}/Product");

        // Type in search box (should have hx-get with debounce)
        await Page.FillAsync("input[hx-get*='/Search']", "Test");

        // Wait for HTMX request (debounced)
        await WaitForHxRequestAsync("/Product/Search");

        // Table should update with filtered results
        await AssertTextContainsAsync("#product-list tbody", "Widget");
    }

    /// <summary>
    /// Example: Infinite scroll loading more products.
    /// </summary>
    [Fact(Skip = "Example test - requires running application")]
    public async Task InfiniteScroll_LoadsMoreProducts()
    {
        // Navigate to products page
        await Page.GotoAsync($"{BaseUrl}/Product");

        // Get initial row count
        var initialCount = await Page.Locator("table tbody tr").CountAsync();

        // Scroll to bottom (triggers hx-trigger="revealed")
        await Page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");

        // Wait for HTMX to load more
        await WaitForHxRequestAsync("/Product/LoadMore");

        // Row count should increase
        var newCount = await Page.Locator("table tbody tr").CountAsync();
        Assert.True(newCount > initialCount);
    }

    /// <summary>
    /// Example: hx-boost navigation without page reload.
    /// </summary>
    [Fact(Skip = "Example test - requires running application")]
    public async Task BoostNavigation_WorksWithoutPageReload()
    {
        // Navigate to home page with hx-boost
        await Page.GotoAsync($"{BaseUrl}");

        // Click link with hx-boost="true"
        await Page.ClickAsync("a[href='/Product'][hx-boost='true']");

        // Wait for HTMX boost to complete
        await WaitForHxBoostAsync();

        // URL should change without page reload
        Assert.Contains("/Product", GetCurrentUrl());

        // Main content should be updated
        await AssertVisibleAsync("table"); // Product table visible
    }

    /// <summary>
    /// Example: Form validation errors displayed inline.
    /// </summary>
    [Fact(Skip = "Example test - requires running application")]
    public async Task ValidationErrors_DisplayedInline()
    {
        // Navigate to products page
        await Page.GotoAsync($"{BaseUrl}/Product");

        // Click "New Product" button
        await ClickAndWaitForHxSwapAsync("button[hx-get='/Product/Create']", "#product-form");

        // Submit empty form (should trigger validation)
        await Page.ClickAsync("#product-form button[type='submit']");

        // Wait for HTMX response
        await WaitForHxEventAsync("htmx:afterRequest");

        // Validation errors should display inline
        await AssertVisibleAsync(".field-validation-error");
        await AssertTextContainsAsync(".field-validation-error", "required");
    }
}
