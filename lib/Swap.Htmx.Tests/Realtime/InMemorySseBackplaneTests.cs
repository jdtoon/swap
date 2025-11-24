using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Swap.Htmx.Realtime;
using Xunit;

namespace Swap.Htmx.Tests.Realtime;

public class InMemorySseBackplaneTests
{
    [Fact]
    public async Task PublishAsync_ShouldInvokeSubscriber()
    {
        // Arrange
        var backplane = new InMemorySseBackplane(NullLogger<InMemorySseBackplane>.Instance);
        var receivedMessage = (SseMessage?)null;
        
        await backplane.SubscribeAsync((msg, ct) =>
        {
            receivedMessage = msg;
            return Task.CompletedTask;
        });

        var message = new SseMessage("test-event", "<div>Content</div>", SseRecipientType.Broadcast);

        // Act
        await backplane.PublishAsync(message);

        // Assert
        Assert.NotNull(receivedMessage);
        Assert.Equal("test-event", receivedMessage.EventName);
        Assert.Equal("<div>Content</div>", receivedMessage.Html);
    }

    [Fact]
    public async Task PublishAsync_ShouldHandleMultipleSubscribers()
    {
        // Arrange
        var backplane = new InMemorySseBackplane(NullLogger<InMemorySseBackplane>.Instance);
        int callCount = 0;

        await backplane.SubscribeAsync((msg, ct) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.CompletedTask;
        });

        await backplane.SubscribeAsync((msg, ct) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.CompletedTask;
        });

        var message = new SseMessage("test-event", "<div>Content</div>", SseRecipientType.Broadcast);

        // Act
        await backplane.PublishAsync(message);

        // Assert
        Assert.Equal(2, callCount);
    }
}
