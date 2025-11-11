using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace Swap.Htmx.E2ETests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class WebSocketTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000/test/websocket";

    [SetUp]
    public async Task Setup()
    {
        await Page.GotoAsync(BaseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    [Test]
    public async Task WebSocket_SendsAndReceivesMessages()
    {
        // Arrange - Set username
        await Page.Locator("input[name='username']").FillAsync("TestUser");
        
        // Act - Send a message
        var messageInput = Page.Locator("input[name='message']");
        await messageInput.FillAsync("Hello from E2E test");
        await Page.Locator("form[id='chat-form']").Locator("button[type='submit']").ClickAsync();

        // Assert - Message appears in chat
        var chatMessages = Page.Locator("#chat-messages");
        await Expect(chatMessages.Locator("text=Hello from E2E test")).ToBeVisibleAsync(new() { Timeout = 3000 });
        
        // Verify username is displayed
        await Expect(chatMessages.Locator("text=TestUser")).ToBeVisibleAsync();
    }

    [Test]
    public async Task WebSocket_MultipleMessagesAppearInOrder()
    {
        // Arrange
        await Page.Locator("input[name='username']").FillAsync("Tester");
        
        // Act - Send multiple messages
        var messages = new[] { "First message", "Second message", "Third message" };
        var messageInput = Page.Locator("input[name='message']");
        var submitButton = Page.Locator("form[id='chat-form']").Locator("button[type='submit']");
        
        foreach (var message in messages)
        {
            await messageInput.FillAsync(message);
            await submitButton.ClickAsync();
            await Task.Delay(100); // Small delay to ensure ordering
        }

        // Assert - All messages are visible
        var chatMessages = Page.Locator("#chat-messages");
        foreach (var message in messages)
        {
            await Expect(chatMessages.Locator($"text={message}")).ToBeVisibleAsync(new() { Timeout = 2000 });
        }

        // Verify count
        var allMessages = chatMessages.Locator(".mb-2");
        var count = await allMessages.CountAsync();
        Assert.That(count, Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public async Task WebSocket_MessageInputClearsAfterSending()
    {
        // Arrange
        await Page.Locator("input[name='username']").FillAsync("User");
        var messageInput = Page.Locator("input[name='message']");
        
        // Act
        await messageInput.FillAsync("Test message");
        await Page.Locator("form[id='chat-form']").Locator("button[type='submit']").ClickAsync();
        
        // Wait a bit for the clear script to run
        await Task.Delay(50);
        
        // Assert - Input should be empty
        var inputValue = await messageInput.InputValueAsync();
        Assert.That(inputValue, Is.Empty);
    }

    [Test]
    public async Task WebSocket_DisplaysTimestampForMessages()
    {
        // Arrange
        await Page.Locator("input[name='username']").FillAsync("TimeTester");
        
        // Act
        var messageInput = Page.Locator("input[name='message']");
        await messageInput.FillAsync("Check timestamp");
        await Page.Locator("form[id='chat-form']").Locator("button[type='submit']").ClickAsync();

        // Assert - Timestamp is displayed
        var chatMessages = Page.Locator("#chat-messages");
        var timestampLocator = chatMessages.Locator(".text-xs.text-gray-500");
        await Expect(timestampLocator.First).ToBeVisibleAsync(new() { Timeout = 2000 });
        
        // Verify timestamp format (HH:mm:ss)
        var timestamp = await timestampLocator.First.TextContentAsync();
        Assert.That(timestamp, Does.Match(@"\d{2}:\d{2}:\d{2}"));
    }

    [Test]
    public async Task WebSocketDemo_DisplaysInstructions()
    {
        // Assert - Verify instructions are visible
        await Expect(Page.Locator("text=WebSocket Chat")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=This demo shows bidirectional WebSocket communication")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Type a message and click Send")).ToBeVisibleAsync();
        
        // Verify form elements exist
        await Expect(Page.Locator("input[name='username']")).ToBeVisibleAsync();
        await Expect(Page.Locator("input[name='message']")).ToBeVisibleAsync();
        await Expect(Page.Locator("button[type='submit']")).ToBeVisibleAsync();
    }

    [Test]
    public async Task WebSocket_MultipleUsersSeeSameMessages()
    {
        // This test opens two browser contexts to simulate multiple users
        var context2 = await Page.Context.Browser!.NewContextAsync();
        var page2 = await context2.NewPageAsync();
        
        try
        {
            // Setup both pages
            await page2.GotoAsync(BaseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page2.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            
            // User 1 sends a message
            await Page.Locator("input[name='username']").FillAsync("User1");
            await Page.Locator("input[name='message']").FillAsync("Message from User1");
            await Page.Locator("form[id='chat-form']").Locator("button[type='submit']").ClickAsync();

            // Both users should see the message
            await Expect(Page.Locator("#chat-messages").Locator("text=Message from User1"))
                .ToBeVisibleAsync(new() { Timeout = 2000 });
            await Expect(page2.Locator("#chat-messages").Locator("text=Message from User1"))
                .ToBeVisibleAsync(new() { Timeout = 2000 });

            // User 2 sends a message
            await page2.Locator("input[name='username']").FillAsync("User2");
            await page2.Locator("input[name='message']").FillAsync("Message from User2");
            await page2.Locator("form[id='chat-form']").Locator("button[type='submit']").ClickAsync();

            // Both users should see both messages
            await Expect(Page.Locator("#chat-messages").Locator("text=Message from User2"))
                .ToBeVisibleAsync(new() { Timeout = 2000 });
            await Expect(page2.Locator("#chat-messages").Locator("text=Message from User2"))
                .ToBeVisibleAsync(new() { Timeout = 2000 });
        }
        finally
        {
            await page2.CloseAsync();
            await context2.CloseAsync();
        }
    }
}
