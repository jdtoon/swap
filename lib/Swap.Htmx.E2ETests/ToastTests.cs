using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace Swap.Htmx.E2ETests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class ToastTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000/test";

    [SetUp]
    // Removed Toast Playwright tests.

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
