using FluentAssertions;
using NetMX.Events;
using Xunit;

namespace NetMX.Core.Tests.Events;

public class EventContextTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var context = new EventContext();

        // Assert
        context.RequestId.Should().NotBe(Guid.Empty);
        context.SessionId.Should().BeEmpty();
        context.UserId.Should().BeNull();
        context.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        context.Depth.Should().Be(0);
        context.OriginEvent.Should().BeNull();
        context.ProcessedEvents.Should().BeEmpty();
        context.EventCount.Should().Be(0);
    }

    [Fact]
    public void Constructor_ShouldAllowCustomInitialization()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var sessionId = "session-123";
        var userId = Guid.NewGuid();

        // Act
        var context = new EventContext
        {
            RequestId = requestId,
            SessionId = sessionId,
            UserId = userId
        };

        // Assert
        context.RequestId.Should().Be(requestId);
        context.SessionId.Should().Be(sessionId);
        context.UserId.Should().Be(userId);
    }

    [Fact]
    public void CreateChild_ShouldIncrementDepth()
    {
        // Arrange
        var parent = new EventContext();

        // Act
        var child = parent.CreateChild("test.event");

        // Assert
        child.Depth.Should().Be(1);
        child.OriginEvent.Should().Be("test.event");
    }

    [Fact]
    public void CreateChild_ShouldPreserveRequestContext()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var sessionId = "session-123";
        var userId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        var parent = new EventContext
        {
            RequestId = requestId,
            SessionId = sessionId,
            UserId = userId,
            Timestamp = timestamp
        };

        // Act
        var child = parent.CreateChild("test.event");

        // Assert
        child.RequestId.Should().Be(requestId);
        child.SessionId.Should().Be(sessionId);
        child.UserId.Should().Be(userId);
        child.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void CreateChild_ShouldShareProcessedEvents()
    {
        // Arrange
        var parent = new EventContext();
        parent.ProcessedEvents.Add("fingerprint-1");
        parent.ProcessedEvents.Add("fingerprint-2");

        // Act
        var child = parent.CreateChild("test.event");

        // Assert
        child.ProcessedEvents.Should().Contain("fingerprint-1");
        child.ProcessedEvents.Should().Contain("fingerprint-2");
    }

    [Fact]
    public void CreateChild_ShouldIncrementEventCount()
    {
        // Arrange
        var parent = new EventContext();
        parent.IncrementEventCount();
        parent.IncrementEventCount();

        // Act
        var child = parent.CreateChild("test.event");

        // Assert
        child.EventCount.Should().Be(3); // Parent had 2, child adds 1
    }

    [Fact]
    public void CreateChild_ShouldThrowWhenMaxDepthExceeded()
    {
        // Arrange
        var context = new EventContext();
        
        // Create 10 levels (max depth)
        for (int i = 0; i < EventContext.MaxDepth; i++)
        {
            context = context.CreateChild($"event-{i}");
        }

        // Act
        var act = () => context.CreateChild("event-overflow");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Event depth exceeded*");
    }

    [Fact]
    public void CreateChild_ShouldThrowWhenEventBudgetExceeded()
    {
        // Arrange
        var context = new EventContext();
        
        // Increment event count to max
        for (int i = 0; i < EventContext.MaxEvents; i++)
        {
            context.IncrementEventCount();
        }

        // Act
        var act = () => context.CreateChild("event-overflow");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Event budget exceeded*");
    }

    [Fact]
    public void IncrementEventCount_ShouldIncreaseCount()
    {
        // Arrange
        var context = new EventContext();

        // Act
        context.IncrementEventCount();
        context.IncrementEventCount();
        context.IncrementEventCount();

        // Assert
        context.EventCount.Should().Be(3);
    }

    [Fact]
    public void ProcessedEvents_ShouldBeUnique()
    {
        // Arrange
        var context = new EventContext();

        // Act
        context.ProcessedEvents.Add("fingerprint-1");
        context.ProcessedEvents.Add("fingerprint-1"); // Duplicate
        context.ProcessedEvents.Add("fingerprint-2");

        // Assert
        context.ProcessedEvents.Should().HaveCount(2);
        context.ProcessedEvents.Should().Contain("fingerprint-1");
        context.ProcessedEvents.Should().Contain("fingerprint-2");
    }

    [Fact]
    public void MaxDepth_ShouldBe10()
    {
        // Assert
        EventContext.MaxDepth.Should().Be(10);
    }

    [Fact]
    public void MaxEvents_ShouldBe50()
    {
        // Assert
        EventContext.MaxEvents.Should().Be(50);
    }

    [Fact]
    public void CreateChild_DepthChain_ShouldTrackOrigin()
    {
        // Arrange
        var root = new EventContext();

        // Act
        var level1 = root.CreateChild("event.level1");
        var level2 = level1.CreateChild("event.level2");
        var level3 = level2.CreateChild("event.level3");

        // Assert
        root.Depth.Should().Be(0);
        root.OriginEvent.Should().BeNull();

        level1.Depth.Should().Be(1);
        level1.OriginEvent.Should().Be("event.level1");

        level2.Depth.Should().Be(2);
        level2.OriginEvent.Should().Be("event.level2");

        level3.Depth.Should().Be(3);
        level3.OriginEvent.Should().Be("event.level3");
    }

    [Fact]
    public void CreateChild_ProcessedEventsMutation_ShouldAffectChild()
    {
        // Arrange
        var parent = new EventContext();
        parent.ProcessedEvents.Add("fingerprint-1");

        // Act
        var child = parent.CreateChild("test.event");
        parent.ProcessedEvents.Add("fingerprint-2"); // Add to parent after child created

        // Assert - child should NOT see the new fingerprint (copied, not shared reference)
        child.ProcessedEvents.Should().Contain("fingerprint-1");
        child.ProcessedEvents.Should().NotContain("fingerprint-2");
    }
}
