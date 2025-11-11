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
        Assert.That(updatedText, Does.Contain("Updated"));
    }

    [Test]
    public async Task OobMultiple_UpdatesAllTargetElements()
    {
        // Arrange - Verify initial states
        var panel1 = Page.Locator("[data-test-id='panel-1']");
        var panel2 = Page.Locator("[data-test-id='panel-2']");
        var panel3 = Page.Locator("[data-test-id='panel-3']");

        var initial1 = await panel1.TextContentAsync();
        var initial2 = await panel2.TextContentAsync();
        var initial3 = await panel3.TextContentAsync();

        Assert.That(initial1, Does.Contain("Panel 1"));
        Assert.That(initial2, Does.Contain("Panel 2"));
        Assert.That(initial3, Does.Contain("Panel 3"));

        // Act - Click update button
        await Page.Locator("[data-test-id='oob-multiple']").ClickAsync(new() { Force = true });

        // Assert - Wait for all panels to update
        await Task.Delay(500);

        var updated1 = await panel1.TextContentAsync();
        var updated2 = await panel2.TextContentAsync();
        var updated3 = await panel3.TextContentAsync();

        Assert.That(updated1, Does.Contain("Panel 1 Updated"));
        Assert.That(updated2, Does.Contain("Panel 2 Updated"));
        Assert.That(updated3, Does.Contain("Panel 3 Updated"));
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
        var counter = Page.Locator("[data-test-id='counter-display']");
        var initialValueText = await counter.TextContentAsync();
        var initialValue = int.Parse(initialValueText!.Replace("Count:", "").Trim());

        // Act - Click button 3 times
        for (int i = 0; i < 3; i++)
        {
            await Page.Locator("[data-test-id='oob-counter']").ClickAsync(new() { Force = true });
            await Task.Delay(300);
        }

        // Assert - Counter should be +3
        var finalValueText = await counter.TextContentAsync();
        var finalValue = int.Parse(finalValueText!.Replace("Counter:", "").Trim());

        Assert.That(finalValue, Is.EqualTo(initialValue + 3));
    }

    [Test]
    public async Task OobSwap_DoesNotAffectMainSwapTarget()
    {
        // Arrange - Get main content area (oob-result div)
        var mainContent = Page.Locator("[data-test-id='oob-result']");
        
        // Act - Trigger OOB update (single update)
        await Page.Locator("[data-test-id='oob-single']").ClickAsync(new() { Force = true });
        await Task.Delay(500);

        // Assert - Main target should have content
        var mainText = await mainContent.TextContentAsync();
        Assert.That(mainText, Does.Contain("Primary content"));

        // OOB target should have changed independently
        var oobTarget = Page.Locator("[data-test-id='secondary-panel']");
        var oobContent = await oobTarget.TextContentAsync();
        Assert.That(oobContent, Does.Contain("Updated"));
    }
}
