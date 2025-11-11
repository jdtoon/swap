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
    public async Task Debug_CheckOobSwapHtml()
    {
        await Page.GotoAsync("http://localhost:5000/test", new() { WaitUntil = WaitUntilState.NetworkIdle });
        
        // Check initial state
        var initialHtml = await Page.Locator("#secondary-panel").InnerHTMLAsync();
        Console.WriteLine($"Initial HTML:\n{initialHtml}");
        
        var initialTestId = await Page.Locator("#secondary-panel").GetAttributeAsync("data-test-id");
        Console.WriteLine($"Initial data-test-id: {initialTestId}");
        
        // Click OOB button
        await Page.Locator("[data-test-id='oob-single']").ClickAsync(new() { Force = true });
        await Page.WaitForTimeoutAsync(2000);
        
        // Check updated state
        var afterCount = await Page.Locator("#secondary-panel").CountAsync();
        Console.WriteLine($"Element count after swap: {afterCount}");
        
        if (afterCount > 0)
        {
            var updatedOuterHtml = await Page.Locator("#secondary-panel").EvaluateAsync<string>("el => el.outerHTML");
            Console.WriteLine($"Updated outer HTML:\n{updatedOuterHtml}");
            
            var updatedTestId = await Page.Locator("#secondary-panel").GetAttributeAsync("data-test-id");
            Console.WriteLine($"Updated data-test-id: {updatedTestId}");
        }
    }
}
