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
    public async Task ToastDemo_LoadsCorrectly()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Toast Notifications" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Trigger Toast" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task ToastDemo_DisplaysToastOnTrigger()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Trigger Toast" }).ClickAsync();
        var toast = Page.Locator("#toast-area > div");
        await Expect(toast).ToBeVisibleAsync(new() { Timeout = 3000 });
        var text = await toast.TextContentAsync();
        Assert.That(text, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task ToastDemo_MultipleToastsStack()
    {
        for (int i = 0; i < 3; i++)
            await Page.GetByRole(AriaRole.Button, new() { Name = "Trigger Toast" }).ClickAsync();

        var toasts = Page.Locator("#toast-area > div");
        await Expect(toasts.Nth(2)).ToBeVisibleAsync(new() { Timeout = 4000 });
        Assert.That(await toasts.CountAsync(), Is.GreaterThanOrEqualTo(3));
    }
}
