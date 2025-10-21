using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NetMX.Events;
using Xunit;

namespace NetMX.Core.Tests.Events;

public class EventBusTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<EventBus>> _loggerMock;
    private readonly EventBus _eventBus;

    public EventBusTests()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        _serviceProvider = services.BuildServiceProvider();
        
        _cache = _serviceProvider.GetRequiredService<IMemoryCache>();
        _loggerMock = new Mock<ILogger<EventBus>>();
        _eventBus = new EventBus(_serviceProvider, _cache, _loggerMock.Object);
    }

    [Fact]
    public async Task PublishAsync_ShouldExecuteHandler()
    {
        // Arrange
        var handlerMock = new Mock<IEventHandler<TestEventData>>();
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddScoped<IEventHandler<TestEventData>>(_ => handlerMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var eventBus = new EventBus(serviceProvider, _cache, _loggerMock.Object);
        var data = new TestEventData { Message = "Test" };
        var context = new EventContext();

        // Act
        await eventBus.PublishAsync("test.event", data, context);

        // Assert
        handlerMock.Verify(
            h => h.HandleAsync("test.event", data, context, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldExecuteMultipleHandlers()
    {
        // Arrange
        var handler1Mock = new Mock<IEventHandler<TestEventData>>();
        var handler2Mock = new Mock<IEventHandler<TestEventData>>();
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddScoped<IEventHandler<TestEventData>>(_ => handler1Mock.Object);
        services.AddScoped<IEventHandler<TestEventData>>(_ => handler2Mock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var eventBus = new EventBus(serviceProvider, _cache, _loggerMock.Object);
        var data = new TestEventData { Message = "Test" };

        // Act
        await eventBus.PublishAsync("test.event", data);

        // Assert
        handler1Mock.Verify(
            h => h.HandleAsync(It.IsAny<string>(), It.IsAny<TestEventData>(), It.IsAny<EventContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
        handler2Mock.Verify(
            h => h.HandleAsync(It.IsAny<string>(), It.IsAny<TestEventData>(), It.IsAny<EventContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldCreateContextIfNull()
    {
        // Arrange
        var handlerMock = new Mock<IEventHandler<TestEventData>>();
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddScoped<IEventHandler<TestEventData>>(_ => handlerMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var eventBus = new EventBus(serviceProvider, _cache, _loggerMock.Object);
        var data = new TestEventData { Message = "Test" };

        // Act
        await eventBus.PublishAsync("test.event", data, context: null);

        // Assert
        handlerMock.Verify(
            h => h.HandleAsync("test.event", data, It.Is<EventContext>(c => c != null), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldDeduplicateSameEvent()
    {
        // Arrange
        var handlerMock = new Mock<IEventHandler<TestEventData>>();
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddScoped<IEventHandler<TestEventData>>(_ => handlerMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var eventBus = new EventBus(serviceProvider, _cache, _loggerMock.Object);
        var data = new TestEventData { Message = "Test" };
        var context = new EventContext();

        // Act
        await eventBus.PublishAsync("test.event", data, context);
        await eventBus.PublishAsync("test.event", data, context); // Same event, same data, same context

        // Assert - Handler should only be called once (deduplicated)
        handlerMock.Verify(
            h => h.HandleAsync(It.IsAny<string>(), It.IsAny<TestEventData>(), It.IsAny<EventContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldNotDeduplicateDifferentEvents()
    {
        // Arrange
        var handlerMock = new Mock<IEventHandler<TestEventData>>();
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddScoped<IEventHandler<TestEventData>>(_ => handlerMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var eventBus = new EventBus(serviceProvider, _cache, _loggerMock.Object);
        var context = new EventContext();

        // Act
        await eventBus.PublishAsync("test.event1", new TestEventData { Message = "Test1" }, context);
        await eventBus.PublishAsync("test.event2", new TestEventData { Message = "Test2" }, context);

        // Assert - Handler should be called twice (different events)
        handlerMock.Verify(
            h => h.HandleAsync(It.IsAny<string>(), It.IsAny<TestEventData>(), It.IsAny<EventContext>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task PublishAsync_ShouldStopAtMaxDepth()
    {
        // Arrange
        var context = new EventContext();
        
        // Create 10 levels (max depth)
        for (int i = 0; i < EventContext.MaxDepth; i++)
        {
            context = context.CreateChild($"event-{i}");
        }

        var data = new TestEventData { Message = "Test" };

        // Act (should not throw, just log warning)
        await _eventBus.PublishAsync("test.event", data, context);

        // Assert - Should log warning about max depth
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Event depth exceeded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldStopAtEventBudget()
    {
        // Arrange
        var context = new EventContext();
        
        // Increment event count to max
        for (int i = 0; i < EventContext.MaxEvents; i++)
        {
            context.IncrementEventCount();
        }

        var data = new TestEventData { Message = "Test" };

        // Act (should not throw, just log warning)
        await _eventBus.PublishAsync("test.event", data, context);

        // Assert - Should log warning about event budget
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Event budget exceeded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldIncrementEventCount()
    {
        // Arrange
        var handlerMock = new Mock<IEventHandler<TestEventData>>();
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddScoped<IEventHandler<TestEventData>>(_ => handlerMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var eventBus = new EventBus(serviceProvider, _cache, _loggerMock.Object);
        var context = new EventContext();
        var initialCount = context.EventCount;

        // Act
        await eventBus.PublishAsync("test.event", new TestEventData { Message = "Test" }, context);

        // Assert
        context.EventCount.Should().Be(initialCount + 1);
    }

    [Fact]
    public async Task PublishAsync_ShouldContinueOnHandlerException()
    {
        // Arrange
        var failingHandlerMock = new Mock<IEventHandler<TestEventData>>();
        failingHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<TestEventData>(), It.IsAny<EventContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Handler failed"));

        var successHandlerMock = new Mock<IEventHandler<TestEventData>>();

        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddScoped<IEventHandler<TestEventData>>(_ => failingHandlerMock.Object);
        services.AddScoped<IEventHandler<TestEventData>>(_ => successHandlerMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var eventBus = new EventBus(serviceProvider, _cache, _loggerMock.Object);

        // Act
        await eventBus.PublishAsync("test.event", new TestEventData { Message = "Test" });

        // Assert - Both handlers should be called despite first one failing
        failingHandlerMock.Verify(
            h => h.HandleAsync(It.IsAny<string>(), It.IsAny<TestEventData>(), It.IsAny<EventContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
        successHandlerMock.Verify(
            h => h.HandleAsync(It.IsAny<string>(), It.IsAny<TestEventData>(), It.IsAny<EventContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTriggeredEvents_ShouldReturnTriggeredEvents()
    {
        // Arrange
        var context = new EventContext();
        var data = new TestEventData { Message = "Test" };

        // Act
        await _eventBus.PublishAsync("test.event", data, context);
        var triggeredEvents = _eventBus.GetTriggeredEvents(context.RequestId);

        // Assert
        triggeredEvents.Should().ContainKey("test.event");
        triggeredEvents["test.event"].Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task GetTriggeredEvents_ShouldRemoveFromTrackingAfterRetrieve()
    {
        // Arrange
        var context = new EventContext();
        var data = new TestEventData { Message = "Test" };
        await _eventBus.PublishAsync("test.event", data, context);

        // Act
        var firstRetrieval = _eventBus.GetTriggeredEvents(context.RequestId);
        var secondRetrieval = _eventBus.GetTriggeredEvents(context.RequestId);

        // Assert
        firstRetrieval.Should().ContainKey("test.event");
        secondRetrieval.Should().BeEmpty(); // Already removed
    }

    [Fact]
    public async Task GetTriggeredEvents_ShouldReturnEmptyForUnknownRequest()
    {
        // Act
        var triggeredEvents = _eventBus.GetTriggeredEvents(Guid.NewGuid());

        // Assert
        triggeredEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task PublishAsync_ShouldTrackMultipleEvents()
    {
        // Arrange
        var context = new EventContext();

        // Act
        await _eventBus.PublishAsync("event1", new TestEventData { Message = "Test1" }, context);
        await _eventBus.PublishAsync("event2", new TestEventData { Message = "Test2" }, context);
        await _eventBus.PublishAsync("event3", new TestEventData { Message = "Test3" }, context);

        var triggeredEvents = _eventBus.GetTriggeredEvents(context.RequestId);

        // Assert
        triggeredEvents.Should().HaveCount(3);
        triggeredEvents.Should().ContainKey("event1");
        triggeredEvents.Should().ContainKey("event2");
        triggeredEvents.Should().ContainKey("event3");
    }
}

// Test event data
public class TestEventData
{
    public string Message { get; set; } = string.Empty;
}
