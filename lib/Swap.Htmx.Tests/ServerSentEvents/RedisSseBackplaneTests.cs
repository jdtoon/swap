using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Swap.Htmx.Realtime;
using Swap.Htmx.Realtime.Redis;
using Xunit;

namespace Swap.Htmx.Tests.ServerSentEvents;

public class RedisSseBackplaneTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<ISubscriber> _mockSubscriber;
    private readonly Mock<ILogger<RedisSseBackplane>> _mockLogger;
    private readonly IOptions<RedisSseOptions> _options;
    private readonly RedisSseBackplane _backplane;

    public RedisSseBackplaneTests()
    {
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockSubscriber = new Mock<ISubscriber>();
        _mockLogger = new Mock<ILogger<RedisSseBackplane>>();
        
        _mockRedis.Setup(r => r.GetSubscriber(It.IsAny<object>())).Returns(_mockSubscriber.Object);
        
        _options = Options.Create(new RedisSseOptions 
        { 
            ChannelName = "test-channel" 
        });

        _backplane = new RedisSseBackplane(_mockRedis.Object, _options, _mockLogger.Object);
    }

    [Fact]
    public async Task PublishAsync_SerializesMessageAndPublishesToRedis()
    {
        // Arrange
        var message = new SseMessage("test-event", "<div>Content</div>", SseRecipientType.Broadcast);
        
        // Act
        await _backplane.PublishAsync(message);

        // Assert
        _mockSubscriber.Verify(s => s.PublishAsync(
            It.Is<RedisChannel>(c => c == "test-channel"), 
            It.Is<RedisValue>(v => v.ToString().Contains("test-event") && v.ToString().Contains("<div>Content</div>")),
            It.IsAny<CommandFlags>()), 
            Times.Once);
    }

    [Fact]
    public async Task SubscribeAsync_SubscribesToRedisChannel()
    {
        // Arrange
        Func<SseMessage, CancellationToken, Task> handler = (msg, token) => Task.CompletedTask;

        // Act
        await _backplane.SubscribeAsync(handler);

        // Assert
        _mockSubscriber.Verify(s => s.SubscribeAsync(
            It.Is<RedisChannel>(c => c == "test-channel"),
            It.IsAny<Action<RedisChannel, RedisValue>>(),
            It.IsAny<CommandFlags>()),
            Times.Once);
    }
}
