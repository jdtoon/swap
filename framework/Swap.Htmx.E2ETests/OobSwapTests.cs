using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace Swap.Htmx.E2ETests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class OobSwapTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000/test";

    [SetUp]
    public async Task Setup()
    {
        await Page.GotoAsync(BaseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    [Test]
    public async Task OobSingle_UpdatesTargetElement()
    {
        // Arrange - Verify initial state
        var initialText = await Page.Locator("[data-test-id='secondary-panel']").TextContentAsync();
        Assert.That(initialText, Does.Contain("Secondary panel"));

        // Act - Click update button
        await Page.Locator("[data-test-id='oob-single']").ClickAsync(new() { Force = true });

        // Assert - Wait for HTMX to complete and verify content changed
        await Page.WaitForTimeoutAsync(1000);
        var updatedText = await Page.Locator("[data-test-id='secondary-panel']").TextContentAsync();
        Assert.That(updatedText, Does.Contain("updated via OOB"));
    }

    [Test]
    public async Task OobMultiple_UpdatesAllTargetElements()
    {
        // Arrange - Verify initial states
        var initial1 = await Page.Locator("[data-test-id='panel-1']").TextContentAsync();
        var initial2 = await Page.Locator("[data-test-id='panel-2']").TextContentAsync();
        var initial3 = await Page.Locator("[data-test-id='panel-3']").TextContentAsync();

        Assert.That(initial1, Does.Contain("Panel 1"));
        Assert.That(initial2, Does.Contain("Panel 2"));
        Assert.That(initial3, Does.Contain("Panel 3"));

        // Act - Click update button
        await Page.Locator("[data-test-id='oob-multiple']").ClickAsync(new() { Force = true });

        // Assert - Wait for HTMX to complete and verify all panels updated
        await Page.WaitForTimeoutAsync(1000);

        var updated1 = await Page.Locator("[data-test-id='panel-1']").TextContentAsync();
        var updated2 = await Page.Locator("[data-test-id='panel-2']").TextContentAsync();
        var updated3 = await Page.Locator("[data-test-id='panel-3']").TextContentAsync();

        Assert.That(updated1, Does.Contain("Panel 1 updated"));
        Assert.That(updated2, Does.Contain("Panel 2 updated"));
        Assert.That(updated3, Does.Contain("Panel 3 updated"));
    }

    [Test]
    public async Task OobCounter_IncrementsValue()
    {
        // Arrange - Get initial counter value
        var counter = Page.Locator("[data-test-id='counter-display']");
        var initialValueText = await counter.TextContentAsync();
        
        // Extract number from "Count: X" format
        var initialValue = int.Parse(initialValueText!.Replace("Count:", "").Trim());

        // Act - Click increment button
        await Page.Locator("[data-test-id='oob-counter']").ClickAsync(new() { Force = true });

        // Assert - Counter should increment
        await Task.Delay(500);
        var updatedValueText = await counter.TextContentAsync();
        var updatedValue = int.Parse(updatedValueText!.Replace("Count:", "").Trim());

        Assert.That(updatedValue, Is.EqualTo(initialValue + 1));
    }

    [Test]
    public async Task OobCounter_IncrementMultipleTimes()
    {
        // Arrange - Get initial value
        var initialValueText = await Page.Locator("[data-test-id='counter-display']").TextContentAsync();
        var initialValue = int.Parse(initialValueText!.Replace("Count:", "").Trim());

        // Act - Click button 3 times
        for (int i = 0; i < 3; i++)
        {
            await Page.Locator("[data-test-id='oob-counter']").ClickAsync(new() { Force = true });
            await Page.WaitForTimeoutAsync(500);
        }

        // Assert - Counter should be +3
        var finalValueText = await Page.Locator("[data-test-id='counter-display']").TextContentAsync();
        var finalValue = int.Parse(finalValueText!.Replace("Count:", "").Trim());

        Assert.That(finalValue, Is.EqualTo(initialValue + 3));
    }

    [Test]
    public async Task OobSwap_DoesNotAffectMainSwapTarget()
    {
        // Act - Trigger OOB update (single update)
        await Page.Locator("[data-test-id='oob-single']").ClickAsync(new() { Force = true });
        await Page.WaitForTimeoutAsync(1000);

        // Assert - Main target should have content
        var mainText = await Page.Locator("[data-test-id='oob-result']").TextContentAsync();
        Assert.That(mainText, Does.Contain("Primary content"));

        // OOB target should have changed independently
        var oobContent = await Page.Locator("[data-test-id='secondary-panel']").TextContentAsync();
        Assert.That(oobContent, Does.Contain("updated"));
    }
}
