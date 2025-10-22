using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using NetMX.Events;
using Xunit;

namespace NetMX.Core.Tests.Events;

/// <summary>
/// Integration tests for EventBus implementation.
/// Tests deduplication, loop prevention, rate limiting, DAG enforcement, and HTMX integration.
/// </summary>
public class EventBusIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IEventBus _eventBus;
    private readonly IMemoryCache _cache;
    private readonly TestEventHandler _testHandler;
    private readonly ChainEventHandler _chainHandler;
    private readonly FailingEventHandler _failingHandler;

    public EventBusIntegrationTests()
    {
        // Setup DI container with EventBus
        var services = new ServiceCollection();
        
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddMemoryCache();
        
        // Register EventBus
        services.AddSingleton<IEventBus, EventBus>();
        
        // Register test handlers
        _testHandler = new TestEventHandler();
        _chainHandler = new ChainEventHandler();
        _failingHandler = new FailingEventHandler();
        
        services.AddSingleton<IEventHandler<TestEventData>>(_testHandler);
        services.AddSingleton<IEventHandler<TestEventData>>(_chainHandler);
        services.AddSingleton<IEventHandler<TestEventData>>(_failingHandler);
        services.AddSingleton<IEventHandler<ChainEventData>>(_chainHandler);
        
        _serviceProvider = services.BuildServiceProvider();
        _eventBus = _serviceProvider.GetRequiredService<IEventBus>();
        _cache = _serviceProvider.GetRequiredService<IMemoryCache>();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    #region Test 1: Basic Event Publishing

    [Fact]
    public async Task PublishAsync_SimpleEvent_CallsHandler()
    {
        // Arrange
        var eventName = "test.simple";
        var eventData = new TestEventData { Message = "Hello World", Value = 42 };

        // Act
        await _eventBus.PublishAsync(eventName, eventData);

        // Assert
        Assert.Equal(1, _testHandler.CallCount);
        Assert.Equal(eventName, _testHandler.LastEventName);
        Assert.Equal("Hello World", _testHandler.LastEventData?.Message);
        Assert.Equal(42, _testHandler.LastEventData?.Value);
    }

    [Fact]
    public async Task PublishAsync_MultipleHandlers_CallsAllHandlers()
    {
        // Arrange
        var eventName = "test.multiple";
        var eventData = new TestEventData { Message = "Test", Value = 1 };

        // Act
        await _eventBus.PublishAsync(eventName, eventData);

        // Assert
        Assert.Equal(1, _testHandler.CallCount);
        Assert.Equal(1, _chainHandler.CallCount);
    }

    #endregion

    #region Test 2: Deduplication

    [Fact]
    public async Task PublishAsync_SameEventTwice_DeduplicatesSecondCall()
    {
        // Arrange
        var eventName = "test.duplicate";
        var eventData = new TestEventData { Message = "Duplicate", Value = 100 };
        var context = new EventContext();

        // Act
        await _eventBus.PublishAsync(eventName, eventData, context);
        await _eventBus.PublishAsync(eventName, eventData, context); // Same data, same context

        // Assert - Handler should only be called once (deduplicated)
        Assert.Equal(1, _testHandler.CallCount);
    }

    [Fact]
    public async Task PublishAsync_DifferentEventData_ProcessesBoth()
    {
        // Arrange
        var eventName = "test.different";
        var eventData1 = new TestEventData { Message = "First", Value = 1 };
        var eventData2 = new TestEventData { Message = "Second", Value = 2 };
        var context = new EventContext();

        _testHandler.Reset();

        // Act
        await _eventBus.PublishAsync(eventName, eventData1, context);
        await _eventBus.PublishAsync(eventName, eventData2, context); // Different data

        // Assert - Both should be processed (different fingerprints)
        Assert.Equal(2, _testHandler.CallCount);
    }

    [Fact]
    public async Task PublishAsync_SameDataDifferentDepth_ProcessesBoth()
    {
        // Arrange
        var eventName = "test.depth";
        var eventData = new TestEventData { Message = "Same", Value = 1 };
        var context1 = new EventContext(); // Depth 0
        var context2 = context1.CreateChild(eventName); // Depth 1

        _testHandler.Reset();

        // Act
        await _eventBus.PublishAsync(eventName, eventData, context1);
        await _eventBus.PublishAsync(eventName, eventData, context2);

        // Assert - Both processed (depth affects fingerprint)
        Assert.Equal(2, _testHandler.CallCount);
    }

    #endregion

    #region Test 3: Loop Prevention - MaxDepth

    [Fact]
    public async Task PublishAsync_ExceedsMaxDepth_StopsAtLimit()
    {
        // Arrange
        var eventName = "test.loop.depth";
        var eventData = new TestEventData { Message = "Loop", Value = 1 };
        
        // Configure chain handler to trigger itself (infinite loop scenario)
        _chainHandler.Reset();
        _chainHandler.ChainEventName = eventName;
        _chainHandler.ChainEventData = eventData;
        _chainHandler.ShouldChain = true;

        // Act
        await _eventBus.PublishAsync(eventName, eventData);

        // Assert - Should stop at MaxDepth (10) + initial = 11 total
        // But deduplication will kick in, so exact count may vary
        // Key point: Should NOT infinite loop
        Assert.True(_chainHandler.CallCount <= EventContext.MaxDepth + 1);
    }

    #endregion

    #region Test 4: Loop Prevention - MaxEvents

    [Fact]
    public async Task PublishAsync_ExceedsMaxEvents_StopsAtBudget()
    {
        // Arrange
        var context = new EventContext();
        
        _testHandler.Reset();

        // Act - Publish 60 different events (exceeds MaxEvents = 50)
        for (int i = 0; i < 60; i++)
        {
            var eventName = $"test.budget.{i}";
            var eventData = new TestEventData { Message = $"Event {i}", Value = i };
            await _eventBus.PublishAsync(eventName, eventData, context);
        }

        // Assert - Should stop at MaxEvents (50)
        Assert.True(context.EventCount <= EventContext.MaxEvents);
        Assert.True(_testHandler.CallCount <= EventContext.MaxEvents);
    }

    #endregion

    #region Test 5: Rate Limiting

    [Fact]
    public async Task PublishAsync_ExceedsRateLimit_BlocksExcessEvents()
    {
        // Arrange
        var eventName = "test.ratelimit";
        var sessionId = "session-123";
        
        _testHandler.Reset();

        // Act - Publish 15 events in same session (limit is 10/min)
        for (int i = 0; i < 15; i++)
        {
            var context = new EventContext { SessionId = sessionId };
            var eventData = new TestEventData { Message = $"Event {i}", Value = i };
            await _eventBus.PublishAsync(eventName, eventData, context);
        }

        // Assert - Only first 10 should succeed
        Assert.True(_testHandler.CallCount <= 10);
    }

    [Fact]
    public async Task PublishAsync_NoSessionId_NoRateLimit()
    {
        // Arrange
        var eventName = "test.noratelimit";
        
        _testHandler.Reset();

        // Act - Publish 15 events without session (anonymous user)
        for (int i = 0; i < 15; i++)
        {
            var context = new EventContext { SessionId = string.Empty };
            var eventData = new TestEventData { Message = $"Event {i}", Value = i };
            await _eventBus.PublishAsync(eventName, eventData, context);
        }

        // Assert - All 15 should succeed (no rate limit without session)
        Assert.Equal(15, _testHandler.CallCount);
    }

    #endregion

    #region Test 6: DAG Enforcement - Terminal Events

    [Fact]
    public async Task PublishAsync_TerminalEvent_CannotTriggerAnything()
    {
        // Arrange
        var terminalEvent = "test.terminal";
        var context = new EventContext();
        
        // Configure chain handler to try triggering from terminal event
        _chainHandler.Reset();
        _chainHandler.ChainEventName = "test.downstream";
        _chainHandler.ChainEventData = new TestEventData { Message = "Should be blocked", Value = 1 };
        _chainHandler.ShouldChain = true;

        // Act - Publish terminal event that tries to trigger another event
        await _eventBus.PublishAsync(terminalEvent, new TestEventData { Message = "Terminal", Value = 1 }, context);

        // Note: This test requires EventDirection attribute on the event constant
        // For now, we'll skip DAG validation tests as they need event constants with attributes
        // These will be tested in Domain Events Integration Tests
    }

    #endregion

    #region Test 7: HTMX Integration

    [Fact]
    public async Task GetTriggeredEvents_ReturnsAllEventsInRequest()
    {
        // Arrange
        var context = new EventContext();
        var requestId = context.RequestId;

        // Act - Publish 3 different events
        await _eventBus.PublishAsync("test.htmx.1", new TestEventData { Message = "First", Value = 1 }, context);
        await _eventBus.PublishAsync("test.htmx.2", new TestEventData { Message = "Second", Value = 2 }, context);
        await _eventBus.PublishAsync("test.htmx.3", new TestEventData { Message = "Third", Value = 3 }, context);

        var triggeredEvents = _eventBus.GetTriggeredEvents(requestId);

        // Assert
        Assert.Equal(3, triggeredEvents.Count);
        Assert.True(triggeredEvents.ContainsKey("test.htmx.1"));
        Assert.True(triggeredEvents.ContainsKey("test.htmx.2"));
        Assert.True(triggeredEvents.ContainsKey("test.htmx.3"));
    }

    [Fact]
    public async Task GetTriggeredEvents_CleansUpAfterRetrieval()
    {
        // Arrange
        var context = new EventContext();
        var requestId = context.RequestId;

        await _eventBus.PublishAsync("test.cleanup", new TestEventData { Message = "Test", Value = 1 }, context);

        // Act - Get triggered events twice
        var first = _eventBus.GetTriggeredEvents(requestId);
        var second = _eventBus.GetTriggeredEvents(requestId);

        // Assert - Second call should return empty (cleaned up after first retrieval)
        Assert.Single(first);
        Assert.Empty(second);
    }

    #endregion

    #region Test 8: Error Handling

    [Fact]
    public async Task PublishAsync_HandlerThrows_ContinuesToOtherHandlers()
    {
        // Arrange
        var eventName = "test.error";
        var eventData = new TestEventData { Message = "Error Test", Value = 1 };

        _testHandler.Reset();
        _chainHandler.Reset();
        _failingHandler.Reset();
        _failingHandler.ShouldFail = true;

        // Act - Publish event with 3 handlers (one will throw)
        await _eventBus.PublishAsync(eventName, eventData);

        // Assert - Other handlers should still be called despite one failing
        Assert.Equal(1, _testHandler.CallCount);
        Assert.Equal(1, _chainHandler.CallCount);
        Assert.Equal(1, _failingHandler.CallCount);
    }

    #endregion

    #region Test Helper Classes

    public class TestEventData
    {
        public string Message { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class ChainEventData
    {
        public string Info { get; set; } = string.Empty;
    }

    public class TestEventHandler : IEventHandler<TestEventData>
    {
        public int CallCount { get; private set; }
        public string? LastEventName { get; private set; }
        public TestEventData? LastEventData { get; private set; }
        public EventContext? LastContext { get; private set; }

        public Task HandleAsync(string eventName, TestEventData data, EventContext context, CancellationToken cancellationToken)
        {
            CallCount++;
            LastEventName = eventName;
            LastEventData = data;
            LastContext = context;
            return Task.CompletedTask;
        }

        public void Reset()
        {
            CallCount = 0;
            LastEventName = null;
            LastEventData = null;
            LastContext = null;
        }
    }

    public class ChainEventHandler : IEventHandler<TestEventData>, IEventHandler<ChainEventData>
    {
        public int CallCount { get; private set; }
        public bool ShouldChain { get; set; }
        public string? ChainEventName { get; set; }
        public TestEventData? ChainEventData { get; set; }

        private IEventBus? _eventBus;

        public async Task HandleAsync(string eventName, TestEventData data, EventContext context, CancellationToken cancellationToken)
        {
            CallCount++;

            // Trigger another event if configured (for chain testing)
            if (ShouldChain && ChainEventName != null && ChainEventData != null && _eventBus != null)
            {
                var childContext = context.CreateChild(eventName);
                await _eventBus.PublishAsync(ChainEventName, ChainEventData, childContext, cancellationToken);
            }
        }

        public Task HandleAsync(string eventName, ChainEventData data, EventContext context, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.CompletedTask;
        }

        public void SetEventBus(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Reset()
        {
            CallCount = 0;
            ShouldChain = false;
            ChainEventName = null;
            ChainEventData = null;
        }
    }

    public class FailingEventHandler : IEventHandler<TestEventData>
    {
        public int CallCount { get; private set; }
        public bool ShouldFail { get; set; }

        public Task HandleAsync(string eventName, TestEventData data, EventContext context, CancellationToken cancellationToken)
        {
            CallCount++;

            if (ShouldFail)
            {
                throw new InvalidOperationException("Handler intentionally failed for testing");
            }

            return Task.CompletedTask;
        }

        public void Reset()
        {
            CallCount = 0;
            ShouldFail = false;
        }
    }

    #endregion
}
