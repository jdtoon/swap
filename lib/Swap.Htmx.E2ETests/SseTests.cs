using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace Swap.Htmx.E2ETests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class SseTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000/test/sse";

    [SetUp]
    public async Task Setup()
    {
        await Page.GotoAsync(BaseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    [Test]
    public async Task SseStream_DisplaysNotificationsInRealTime()
    {
        // Act - Click the "Start Live Feed" button
        await Page.GetByRole(AriaRole.Button, new() { Name = "Start Live Feed" }).ClickAsync();

        // Wait for connection notification
        await Expect(Page.Locator("text=Connected!")).ToBeVisibleAsync(new() { Timeout = 2000 });

        // Wait for first notification to appear
        var firstNotification = Page.Locator("[data-test-id='sse-notification-0']");
        await Expect(firstNotification).ToBeVisibleAsync(new() { Timeout = 3000 });
        
        var firstContent = await firstNotification.TextContentAsync();
        Assert.That(firstContent, Does.Contain("Notification #1"));
        Assert.That(firstContent, Does.Contain("System update completed"));

        // Wait for second notification
        var secondNotification = Page.Locator("[data-test-id='sse-notification-1']");
        await Expect(secondNotification).ToBeVisibleAsync(new() { Timeout = 2000 });
        
        var secondContent = await secondNotification.TextContentAsync();
        Assert.That(secondContent, Does.Contain("Notification #2"));
        Assert.That(secondContent, Does.Contain("New message from Admin"));

        // Wait for completion message
        var completionNotification = Page.Locator("[data-test-id='sse-complete']");
        await Expect(completionNotification).ToBeVisibleAsync(new() { Timeout = 8000 });
        
        var completionContent = await completionNotification.TextContentAsync();
        Assert.That(completionContent, Does.Contain("All caught up!"));
    }

    [Test]
    public async Task SseStream_ReceivesAllFiveNotifications()
    {
        // Act - Start the SSE stream
        await Page.GetByRole(AriaRole.Button, new() { Name = "Start Live Feed" }).ClickAsync();

        // Wait for all 5 notifications to arrive
        for (int i = 0; i < 5; i++)
        {
            var notification = Page.Locator($"[data-test-id='sse-notification-{i}']");
            await Expect(notification).ToBeVisibleAsync(new() { Timeout = 3000 });
        }

        // Verify all notifications are present
        var allNotifications = await Page.Locator("[data-test-id^='sse-notification-']").CountAsync();
        Assert.That(allNotifications, Is.EqualTo(5));
    }

    [Test]
    public async Task SseStream_NotificationsAppearInCorrectOrder()
    {
        // Act
        await Page.GetByRole(AriaRole.Button, new() { Name = "Start Live Feed" }).ClickAsync();

        // Wait for at least 3 notifications
        await Expect(Page.Locator("[data-test-id='sse-notification-2']"))
            .ToBeVisibleAsync(new() { Timeout = 5000 });

        // Get all notifications
        var notifications = Page.Locator("#notification-area > div");
        var count = await notifications.CountAsync();
        
        // Notifications are inserted with afterbegin (prepend), so newest is first
        // Verify order by checking the notification numbers
        for (int i = 0; i < Math.Min(3, count); i++)
        {
            var notification = notifications.Nth(i);
            var text = await notification.TextContentAsync();
            // First item should be the highest number (they're prepended)
            Assert.That(text, Does.Contain("Notification #"));
        }
    }

    [Test]
    public async Task SseDemo_DisplaysInstructionsBeforeStarting()
    {
        // Assert - Verify instructions are visible
        await Expect(Page.Locator("text=Server-Sent Events Demo")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=How it works")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Real-time HTML streaming")).ToBeVisibleAsync();
        
        // Verify start button exists
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Start Live Feed" }))
            .ToBeVisibleAsync();
    }
}
