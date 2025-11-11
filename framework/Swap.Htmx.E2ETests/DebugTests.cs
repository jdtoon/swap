using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace Swap.Htmx.E2ETests;

[TestFixture]
public class DebugTests : PageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            // Headed mode for debugging
            //  headless = false
        };
    }

    [Test]
    public async Task Debug_PageLoads()
    {
        await Page.GotoAsync("http://localhost:5000/test", new() { WaitUntil = WaitUntilState.NetworkIdle });
        
        // Take screenshot
        await Page.ScreenshotAsync(new() { Path = "debug-page.png" });
        
        // Get page content
        var content = await Page.ContentAsync();
        Console.WriteLine($"Page content length: {content.Length}");
        Console.WriteLine($"Page title: {await Page.TitleAsync()}");
        
        // Try to find buttons
        var buttons = await Page.Locator("button").AllAsync();
        Console.WriteLine($"Found {buttons.Count} buttons");
        
        // Try to find by test ID
        var toastButton = Page.GetByTestId("toast-success");
        var isVisible = await toastButton.IsVisibleAsync();
        Console.WriteLine($"Toast button visible: {isVisible}");
        
        if (!isVisible)
        {
            // Print all elements with data-test-id
            var testIds = await Page.Locator("[data-test-id]").AllAsync();
            Console.WriteLine($"Found {testIds.Count} elements with data-test-id");
            foreach (var element in testIds)
            {
                var testId = await element.GetAttributeAsync("data-test-id");
                Console.WriteLine($"  - {testId}");
            }
        }
    }
}
