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
        var initialOobContent = await Page.Locator("[data-test-id='status-panel']").TextContentAsync();
        Assert.That(initialOobContent, Does.Contain("Status: Ready"));

        // Act - Click combined action button
        await Page.Locator("[data-test-id='combined']").ClickAsync(new() { Force = true });

        // Assert - Toast should appear
        var toast = Page.Locator(".toast").First;
        await Expect(toast).ToBeVisibleAsync(new() { Timeout = 5000 });
        var toastContent = await toast.TextContentAsync();
        Assert.That(toastContent, Does.Contain("Both toast"));

        // Assert - OOB target should update (query fresh after HTMX swap)
        await Page.WaitForTimeoutAsync(1000);
        var updatedOobContent = await Page.Locator("[data-test-id='status-panel']").TextContentAsync();
        Assert.That(updatedOobContent, Does.Contain("Last updated"));
    }

    [Test]
    public async Task Combined_MainSwapTargetAndOobBothUpdate()
    {
        // Act - Click combined button
        await Page.Locator("[data-test-id='combined']").ClickAsync(new() { Force = true });
        await Page.WaitForTimeoutAsync(1000);

        // Assert - Main target should show result (query fresh after HTMX swap)
        var mainContent = await Page.Locator("[data-test-id='oob-result']").TextContentAsync();
        Assert.That(mainContent, Does.Contain("Combined"));

        // Assert - OOB target should also update
        var oobContent = await Page.Locator("[data-test-id='status-panel']").TextContentAsync();
        Assert.That(oobContent, Does.Contain("Last updated"));
    }

    [Test]
    public async Task Combined_ToastAndOobIndependent()
    {
        // This test verifies that toast dismissal doesn't affect OOB content
            [Test]
            public async Task Todo_create_emits_events_and_renders_item()
            {
                await Page.GotoAsync(TestUrl("/test"));

                var input = Page.Locator("input[name='title']");
                await input.FillAsync("Buy milk");
                await Page.Locator("button[data-test-id='todo-create']").ClickAsync();

                var toast = Page.Locator("[data-test-id='toast-message']");
                await Expect(toast).ToBeVisibleAsync();
                await Expect(toast).ToContainTextAsync("Buy milk");
            }
        
        // Act - Trigger combined action
        await Page.Locator("[data-test-id='combined']").ClickAsync(new() { Force = true });

        // Assert - Both toast and OOB update appear
        var toast = Page.Locator(".toast").First;
        await Expect(toast).ToBeVisibleAsync(new() { Timeout = 5000 });

        await Page.WaitForTimeoutAsync(1000);
        var oobContent = await Page.Locator("[data-test-id='status-panel']").TextContentAsync();
        Assert.That(oobContent, Does.Contain("Last updated"));

        // Wait for toast to auto-dismiss
        await Page.WaitForTimeoutAsync(4000);
        await Expect(toast).Not.ToBeVisibleAsync();

        // OOB content should still be there (query fresh)
        var oobContentAfter = await Page.Locator("[data-test-id='status-panel']").TextContentAsync();
        Assert.That(oobContentAfter, Does.Contain("Last updated"));
    }

    [Test]
    public async Task MultipleCombinedActions_AccumulateToasts()
    {
        // Act - Trigger combined action twice
        await Page.Locator("[data-test-id='combined']").ClickAsync(new() { Force = true });
        await Page.WaitForTimeoutAsync(200);
        await Page.Locator("[data-test-id='combined']").ClickAsync(new() { Force = true });

        // Assert - Should have 2 toasts visible
        var toasts = Page.Locator(".toast");
        await Expect(toasts).ToHaveCountAsync(2, new() { Timeout = 5000 });

        // OOB target should show latest update (not duplicated) - query fresh
        await Page.WaitForTimeoutAsync(1000);
        var oobContent = await Page.Locator("[data-test-id='status-panel']").TextContentAsync();
        Assert.That(oobContent, Does.Contain("Last updated"));
    }
}
