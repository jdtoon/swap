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
		await Page.GotoAsync(BaseUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
	}

	[Test]
	public async Task SseDemo_Loads() => await Expect(Page.Locator("text=Server-Sent Events Demo")).ToBeVisibleAsync();

	[Test]
	public async Task SseStream_FirstNotificationAppears()
	{
		await Page.GetByRole(AriaRole.Button, new() { Name = "Start Live Feed" }).ClickAsync();
		var first = Page.Locator("[data-test-id='sse-notification-0']");
		await Expect(first).ToBeVisibleAsync(new() { Timeout = 5000 });
		Assert.That(await first.TextContentAsync(), Does.Contain("Notification #1"));
	}
}
