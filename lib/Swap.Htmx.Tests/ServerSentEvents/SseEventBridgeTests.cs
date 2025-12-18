using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Swap.Htmx.Events;
using Swap.Htmx.Realtime;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Swap.Htmx.Tests.ServerSentEvents;

public class SseEventBridgeTests
{
    [Fact]
    public async Task HandleSseEventAsync_BroadcastsEvent()
    {
        // Arrange
        var mockRegistry = new Mock<IRealtimeConnectionRegistry>();
        var mockViewRenderer = new Mock<ISseViewRenderer>();
        
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        var serviceProvider = services.BuildServiceProvider();

        var options = new SwapEventBusOptions();
        var logger = new Mock<ILogger<RealtimeEventBridge>>().Object;

        var bridge = new RealtimeEventBridge(
            mockRegistry.Object,
            serviceProvider,
            options,
            logger,
            mockViewRenderer.Object);

        // Act
        await bridge.HandleSseEventAsync("sse:broadcast:test-event", null);

        // Assert
        mockRegistry.Verify(r => r.BroadcastAsync(
            "test-event",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task HandleSseEventAsync_RendersConfiguredPartials()
    {
        // Arrange
        var mockRegistry = new Mock<IRealtimeConnectionRegistry>();
        var mockViewRenderer = new Mock<ISseViewRenderer>();
        mockViewRenderer.Setup(r => r.RenderPartialAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<div>Rendered Content</div>");

        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        var serviceProvider = services.BuildServiceProvider();

        var options = new SwapEventBusOptions();
        options.When(SseEvents.Broadcast("stats-update"))
            .RefreshPartial("dashboard-stats", "~/Views/Dashboard/Stats.cshtml", _ => new { Total = 42 });

        var logger = new Mock<ILogger<RealtimeEventBridge>>().Object;

        var bridge = new RealtimeEventBridge(
            mockRegistry.Object,
            serviceProvider,
            options,
            logger,
            mockViewRenderer.Object);

        // Act
        await bridge.HandleSseEventAsync("sse:broadcast:stats-update", null);

        // Assert
        mockViewRenderer.Verify(r => r.RenderPartialAsync(
            "~/Views/Dashboard/Stats.cshtml",
            It.IsAny<object>()),
            Times.Once);

        mockRegistry.Verify(r => r.BroadcastAsync(
            "stats-update",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleSseEventAsync_BroadcastsToSpecificRooms()
    {
        // Arrange
        var mockRegistry = new Mock<IRealtimeConnectionRegistry>();
        var mockViewRenderer = new Mock<ISseViewRenderer>();
        mockViewRenderer.Setup(r => r.RenderPartialAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<div>Room Content</div>");

        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        var serviceProvider = services.BuildServiceProvider();

        var options = new SwapEventBusOptions();
        options.When(SseEvents.Rooms("team-alpha").Send("team-update"))
            .RefreshPartial("team-info", "~/Views/Teams/Info.cshtml", _ => new { Team = "alpha" });

        var logger = new Mock<ILogger<RealtimeEventBridge>>().Object;

        var bridge = new RealtimeEventBridge(
            mockRegistry.Object,
            serviceProvider,
            options,
            logger,
            mockViewRenderer.Object);

        // Act
        await bridge.HandleSseEventAsync("sse:rooms:team-alpha:team-update", null);

        // Assert
        mockRegistry.Verify(r => r.BroadcastToRoomsAsync(
            "team-update",
            It.IsAny<string>(),
            It.IsAny<string[]>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleSseEventAsync_BroadcastsToSpecificUser()
    {
        // Arrange
        var mockRegistry = new Mock<IRealtimeConnectionRegistry>();
        var mockViewRenderer = new Mock<ISseViewRenderer>();
        mockViewRenderer.Setup(r => r.RenderPartialAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<div>User Content</div>");

        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        var serviceProvider = services.BuildServiceProvider();

        var options = new SwapEventBusOptions();
        options.When(SseEvents.User("notification", "user123"))
            .RefreshPartial("notification-badge", "~/Views/Notifications/Badge.cshtml", _ => new { Count = 5 });

        var logger = new Mock<ILogger<RealtimeEventBridge>>().Object;

        var bridge = new RealtimeEventBridge(
            mockRegistry.Object,
            serviceProvider,
            options,
            logger,
            mockViewRenderer.Object);

        // Act
        await bridge.HandleSseEventAsync("sse:user:user123:notification", null);

        // Assert
        mockRegistry.Verify(r => r.BroadcastToUserAsync(
            "notification",
            It.IsAny<string>(),
            "user123",
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleSseEventAsync_HandlesMultiplePartials()
    {
        // Arrange
        var mockRegistry = new Mock<IRealtimeConnectionRegistry>();
        var mockViewRenderer = new Mock<ISseViewRenderer>();
        mockViewRenderer.Setup(r => r.RenderPartialAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((string view, object model) => $"<div>{view}</div>");

        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        var serviceProvider = services.BuildServiceProvider();

        var options = new SwapEventBusOptions();
        options.When(SseEvents.Broadcast("multi-update"))
            .RefreshPartial("stats", "~/Views/Dashboard/Stats.cshtml", _ => new { })
            .RefreshPartial("activity", "~/Views/Dashboard/Activity.cshtml", _ => new { });

        var logger = new Mock<ILogger<RealtimeEventBridge>>().Object;

        var bridge = new RealtimeEventBridge(
            mockRegistry.Object,
            serviceProvider,
            options,
            logger,
            mockViewRenderer.Object);

        // Act
        await bridge.HandleSseEventAsync("sse:broadcast:multi-update", null);

        // Assert
        mockViewRenderer.Verify(r => r.RenderPartialAsync(
            It.IsAny<string>(),
            It.IsAny<object>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task HandleSseEventAsync_UsesPayloadInModel()
    {
        // Arrange
        var mockRegistry = new Mock<IRealtimeConnectionRegistry>();
        var mockViewRenderer = new Mock<ISseViewRenderer>();
        
        object? capturedModel = null;
        mockViewRenderer.Setup(r => r.RenderPartialAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Callback<string, object>((view, model) => capturedModel = model)
            .ReturnsAsync("<div>Content</div>");

        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        var serviceProvider = services.BuildServiceProvider();

        var options = new SwapEventBusOptions();
        options.When(SseEvents.Broadcast("task-created"))
            .RefreshPartial("task-list", "~/Views/Tasks/List.cshtml", (ctx, payload) =>
            {
                var taskPayload = payload as TaskPayload;
                return new { TaskId = taskPayload?.TaskId ?? "default" };
            });

        var logger = new Mock<ILogger<RealtimeEventBridge>>().Object;

        var bridge = new RealtimeEventBridge(
            mockRegistry.Object,
            serviceProvider,
            options,
            logger,
            mockViewRenderer.Object);

        var payload = new TaskPayload { TaskId = "task-123" };

        // Act
        await bridge.HandleSseEventAsync("sse:broadcast:task-created", payload);

        // Assert
        Assert.NotNull(capturedModel);
        var taskId = capturedModel.GetType().GetProperty("TaskId")?.GetValue(capturedModel);
        Assert.Equal("task-123", taskId);
    }

    [Fact]
    public async Task HandleSseEventAsync_BroadcastsToSpecificRole()
    {
        // Arrange
        var mockRegistry = new Mock<IRealtimeConnectionRegistry>();
        var mockViewRenderer = new Mock<ISseViewRenderer>();
        mockViewRenderer.Setup(r => r.RenderPartialAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<div>Admin Content</div>");

        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        var serviceProvider = services.BuildServiceProvider();

        var options = new SwapEventBusOptions();
        options.When(SseEvents.Roles("system-alert", "Admin"))
            .RefreshPartial("alert-box", "~/Views/Shared/Alert.cshtml", _ => new { Message = "Alert" });

        var logger = new Mock<ILogger<RealtimeEventBridge>>().Object;

        var bridge = new RealtimeEventBridge(
            mockRegistry.Object,
            serviceProvider,
            options,
            logger,
            mockViewRenderer.Object);

        // Act
        await bridge.HandleSseEventAsync("sse:roles:Admin:system-alert", null);

        // Assert
        mockRegistry.Verify(r => r.BroadcastToRolesAsync(
            "system-alert",
            It.IsAny<string>(),
            It.Is<IEnumerable<string>>(roles => roles.Contains("Admin")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleSseEventAsync_FilterAuthenticated_BroadcastsWithPredicate()
    {
        var mockRegistry = new Mock<IRealtimeConnectionRegistry>();
        var mockViewRenderer = new Mock<ISseViewRenderer>();
        mockViewRenderer.Setup(r => r.RenderPartialAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<div>Filtered</div>");

        Func<IRealtimeConnection, bool>? captured = null;
        mockRegistry
            .Setup(r => r.BroadcastToFilteredAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<IRealtimeConnection, bool>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Func<IRealtimeConnection, bool>, CancellationToken>((_, _, filter, _) => captured = filter)
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        var serviceProvider = services.BuildServiceProvider();

        var options = new SwapEventBusOptions();
        options.When(SseEvents.Filter("auth-event", "authenticated"))
            .RefreshPartial("x", "~/Views/X.cshtml", _ => new { });

        var logger = new Mock<ILogger<RealtimeEventBridge>>().Object;
        var bridge = new RealtimeEventBridge(
            mockRegistry.Object,
            serviceProvider,
            options,
            logger,
            mockViewRenderer.Object);

        await bridge.HandleSseEventAsync("sse:filter:authenticated:auth-event", null);

        Assert.NotNull(captured);

        var authed = new TestRealtimeConnection(CreateAuthenticatedPrincipal("u1"));
        var anon = new TestRealtimeConnection(new ClaimsPrincipal(new ClaimsIdentity()));
        Assert.True(captured!(authed));
        Assert.False(captured!(anon));
    }

    [Fact]
    public async Task HandleSseEventAsync_FilterMonitoring_BroadcastsWithPredicate()
    {
        var mockRegistry = new Mock<IRealtimeConnectionRegistry>();
        var mockViewRenderer = new Mock<ISseViewRenderer>();
        mockViewRenderer.Setup(r => r.RenderPartialAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<div>Filtered</div>");

        Func<IRealtimeConnection, bool>? captured = null;
        mockRegistry
            .Setup(r => r.BroadcastToFilteredAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<IRealtimeConnection, bool>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Func<IRealtimeConnection, bool>, CancellationToken>((_, _, filter, _) => captured = filter)
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        var serviceProvider = services.BuildServiceProvider();

        var options = new SwapEventBusOptions();
        options.When(SseEvents.Filter("mon-event", "monitoring"))
            .RefreshPartial("x", "~/Views/X.cshtml", _ => new { });

        var logger = new Mock<ILogger<RealtimeEventBridge>>().Object;
        var bridge = new RealtimeEventBridge(
            mockRegistry.Object,
            serviceProvider,
            options,
            logger,
            mockViewRenderer.Object);

        await bridge.HandleSseEventAsync("sse:filter:monitoring:mon-event", null);

        Assert.NotNull(captured);

        var monitoring = new TestRealtimeConnection(CreateAuthenticatedPrincipal("u1"), rooms: new[] { "monitoring" });
        var other = new TestRealtimeConnection(CreateAuthenticatedPrincipal("u2"), rooms: new[] { "other" });
        Assert.True(captured!(monitoring));
        Assert.False(captured!(other));
    }
}

public class TaskPayload
{
    public string TaskId { get; set; } = string.Empty;
}

internal sealed class TestRealtimeConnection : IRealtimeConnection
{
    private readonly HashSet<string> _rooms;
    private readonly HashSet<string> _events;

    public TestRealtimeConnection(ClaimsPrincipal? user, IEnumerable<string>? rooms = null, IEnumerable<string>? events = null)
    {
        User = user;
        ConnectedAt = DateTime.UtcNow;
        _rooms = rooms != null ? new HashSet<string>(rooms) : new HashSet<string>();
        _events = events != null ? new HashSet<string>(events) : new HashSet<string>();
        Id = Guid.NewGuid().ToString("N");
    }

    public DateTime ConnectedAt { get; }
    public string Id { get; }
    public bool IsActive => true;
    public IReadOnlyCollection<string> Rooms => _rooms;
    public IReadOnlyCollection<string> SubscribedEvents => _events;
    public ClaimsPrincipal? User { get; }

    public bool IsInRoom(string room) => _rooms.Contains(room);
    public bool IsSubscribedToEvent(string eventName) => _events.Contains(eventName);
    public void JoinRoom(string room) => _rooms.Add(room);
    public void LeaveRoom(string room) => _rooms.Remove(room);
    public Task SendEventAsync(string eventName, string data) => Task.CompletedTask;
    public void SubscribeToEvent(string eventName) => _events.Add(eventName);
    public void UnsubscribeFromEvent(string eventName) => _events.Remove(eventName);
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal static ClaimsPrincipal CreateAuthenticatedPrincipal(string name)
{
    var identity = new ClaimsIdentity(
        new[] { new Claim(ClaimTypes.Name, name) },
        authenticationType: "TestAuth",
        nameType: ClaimTypes.Name,
        roleType: ClaimTypes.Role);
    return new ClaimsPrincipal(identity);
}

