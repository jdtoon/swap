using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace Swap.Htmx.E2ETests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class ToastTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000/test";

    [SetUp]
    public async Task Setup()
    {
        await Page.GotoAsync(BaseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    [Test]
    public async Task ShowSuccessToast_DisplaysToastWithSuccessMessage()
    {
        // Act - Click the success toast button (force = true bypasses visibility check)
        await Page.Locator("[data-test-id='toast-success']").ClickAsync(new() { Force = true });

        // Assert - Wait for toast to appear and verify content
        var toast = Page.Locator(".toast").First;
        await Expect(toast).ToBeVisibleAsync(new() { Timeout = 5000 });
        
        var toastContent = await toast.TextContentAsync();
        Assert.That(toastContent, Does.Contain("Success"));
    }

    [Test]
    public async Task ShowErrorToast_DisplaysToastWithErrorMessage()
    {
        // Act - Click the error toast button
        await Page.Locator("[data-test-id='toast-error']").ClickAsync(new() { Force = true });

        // Assert - Wait for toast to appear and verify content
        var toast = Page.Locator(".toast").First;
        await Expect(toast).ToBeVisibleAsync(new() { Timeout = 5000 });
        
        var toastContent = await toast.TextContentAsync();
        Assert.That(toastContent, Does.Contain("Error"));
    }

    [Test]
    public async Task ShowWarningToast_DisplaysToastWithWarningMessage()
    {
        // Act - Click the warning toast button
        await Page.Locator("[data-test-id='toast-warning']").ClickAsync(new() { Force = true });

        // Assert - Wait for toast to appear and verify content
        var toast = Page.Locator(".toast").First;
        await Expect(toast).ToBeVisibleAsync(new() { Timeout = 5000 });
        
        var toastContent = await toast.TextContentAsync();
        Assert.That(toastContent, Does.Contain("Warning"));
    }

    [Test]
    public async Task ShowInfoToast_DisplaysToastWithInfoMessage()
    {
        // Act - Click the info toast button
        await Page.Locator("[data-test-id='toast-info']").ClickAsync(new() { Force = true });

        // Assert - Wait for toast to appear and verify content
        var toast = Page.Locator(".toast").First;
        await Expect(toast).ToBeVisibleAsync(new() { Timeout = 5000 });
        
        var toastContent = await toast.TextContentAsync();
        Assert.That(toastContent, Does.Contain("Info"));
    }

    [Test]
    public async Task MultipleToasts_DisplaySimultaneously()
    {
        // Act - Click multiple toast buttons
        await Page.Locator("[data-test-id='toast-success']").ClickAsync(new() { Force = true });
        await Task.Delay(100);
        await Page.Locator("[data-test-id='toast-error']").ClickAsync(new() { Force = true });

        // Assert - Both toasts should be visible
        var toasts = Page.Locator(".toast");
        await Expect(toasts).ToHaveCountAsync(2, new() { Timeout = 5000 });
    }

    [Test]
    public async Task Toast_AutoDismissesAfterDelay()
    {
        // Act - Click toast button
        await Page.Locator("[data-test-id='toast-success']").ClickAsync(new() { Force = true });

        // Assert - Toast should appear
        var toast = Page.Locator(".toast").First;
        await Expect(toast).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Wait for auto-dismiss (3.5 seconds + buffer)
        await Task.Delay(4000);

        // Toast should be gone
        await Expect(toast).Not.ToBeVisibleAsync();
    }
}
