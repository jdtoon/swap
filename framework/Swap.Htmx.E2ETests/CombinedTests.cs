using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace Swap.Htmx.E2ETests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class CombinedTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000/test";

    [SetUp]
    public async Task Setup()
    {
        await Page.GotoAsync(BaseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    [Test]
    public async Task Combined_ShowsToastAndUpdatesOob()
    {
        // Arrange - Get OOB target initial state
        var oobTarget = Page.Locator("[data-test-id='status-panel']");
        var initialOobContent = await oobTarget.TextContentAsync();
        Assert.That(initialOobContent, Does.Contain("Status: Ready"));

        // Act - Click combined action button
        await Page.Locator("[data-test-id='combined']").ClickAsync(new() { Force = true });

        // Assert - Toast should appear
        var toast = Page.Locator(".toast").First;
        await Expect(toast).ToBeVisibleAsync(new() { Timeout = 5000 });
        var toastContent = await toast.TextContentAsync();
        Assert.That(toastContent, Does.Contain("Both toast"));

        // Assert - OOB target should update
        await Task.Delay(500);
        var updatedOobContent = await oobTarget.TextContentAsync();
        Assert.That(updatedOobContent, Does.Contain("Last updated"));
    }

    [Test]
    public async Task Combined_MainSwapTargetAndOobBothUpdate()
    {
        // Arrange - Verify initial main target content
        var mainTarget = Page.Locator("[data-test-id='oob-result']");
        
        // Act - Click combined button
        await Page.Locator("[data-test-id='combined']").ClickAsync(new() { Force = true });
        await Task.Delay(500);

        // Assert - Main target should show result
        var mainContent = await mainTarget.TextContentAsync();
        Assert.That(mainContent, Does.Contain("Combined"));

        // Assert - OOB target should also update
        var oobTarget = Page.Locator("[data-test-id='status-panel']");
        var oobContent = await oobTarget.TextContentAsync();
        Assert.That(oobContent, Does.Contain("Last updated"));
    }

    [Test]
    public async Task Combined_ToastAndOobIndependent()
    {
        // This test verifies that toast dismissal doesn't affect OOB content
        
        // Act - Trigger combined action
        await Page.Locator("[data-test-id='combined']").ClickAsync(new() { Force = true });

        // Assert - Both toast and OOB update appear
        var toast = Page.Locator(".toast").First;
        await Expect(toast).ToBeVisibleAsync(new() { Timeout = 5000 });

        var oobTarget = Page.Locator("[data-test-id='status-panel']");
        var oobContent = await oobTarget.TextContentAsync();
        Assert.That(oobContent, Does.Contain("Last updated"));

        // Wait for toast to auto-dismiss
        await Task.Delay(4000);
        await Expect(toast).Not.ToBeVisibleAsync();

        // OOB content should still be there
        var oobContentAfter = await oobTarget.TextContentAsync();
        Assert.That(oobContentAfter, Does.Contain("Last updated"));
    }

    [Test]
    public async Task MultipleCombinedActions_AccumulateToasts()
    {
        // Act - Trigger combined action twice
        await Page.Locator("[data-test-id='combined']").ClickAsync(new() { Force = true });
        await Task.Delay(200);
        await Page.Locator("[data-test-id='combined']").ClickAsync(new() { Force = true });

        // Assert - Should have 2 toasts visible
        var toasts = Page.Locator(".toast");
        await Expect(toasts).ToHaveCountAsync(2, new() { Timeout = 5000 });

        // OOB target should show latest update (not duplicated)
        var oobTarget = Page.Locator("[data-test-id='status-panel']");
        var oobContent = await oobTarget.TextContentAsync();
        Assert.That(oobContent, Does.Contain("Last updated"));
    }
}
