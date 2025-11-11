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
    public async Task Debug_OobSwapResponse()
    {
        await Page.GotoAsync("http://localhost:5000/test", new() { WaitUntil = WaitUntilState.NetworkIdle });
        
        // Set up response listener
        Page.Response += (_, response) =>
        {
            if (response.Url.Contains("/test/oob/single"))
            {
                Console.WriteLine($"Response status: {response.Status}");
                var task = response.BodyAsync();
                task.Wait();
                var body = task.Result;
                var bodyText = System.Text.Encoding.UTF8.GetString(body);
                Console.WriteLine($"Response body:\n{bodyText}");
            }
        };
        
        // Trigger OOB swap
        await Page.Locator("[data-test-id='oob-single']").ClickAsync(new() { Force = true });
        await Page.WaitForTimeoutAsync(2000);
        
        // Check if secondary-panel exists with data-test-id
        var secondaryPanel = Page.Locator("#secondary-panel");
        var exists = await secondaryPanel.CountAsync() > 0;
        Console.WriteLine($"Secondary panel exists: {exists}");
        
        if (exists)
        {
            var testId = await secondaryPanel.GetAttributeAsync("data-test-id");
            Console.WriteLine($"Secondary panel data-test-id: {testId}");
            var content = await secondaryPanel.TextContentAsync();
            Console.WriteLine($"Secondary panel content: {content}");
        }
    }
}
