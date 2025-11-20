using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Swap.Htmx.Events;
using Swap.Htmx.Realtime;
using Xunit;
using Microsoft.AspNetCore.Http;

namespace Swap.Htmx.Tests.ServerSentEvents;

public class SseEventBridgeTests
{
    [Fact]
    public async Task HandleSseEventAsync_BroadcastsEvent()
    {
        // Arrange
        var mockRegistry = new Mock<ISseConnectionRegistry>();
        var mockViewRenderer = new Mock<ISseViewRenderer>();
        
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        var serviceProvider = services.BuildServiceProvider();

        var options = new SwapEventBusOptions();
        var logger = new Mock<ILogger<SseEventBridge>>().Object;

        var bridge = new SseEventBridge(
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
        var mockRegistry = new Mock<ISseConnectionRegistry>();
        var mockViewRenderer = new Mock<ISseViewRenderer>();
        mockViewRenderer.Setup(r => r.RenderPartialAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<div>Rendered Content</div>");

        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        var serviceProvider = services.BuildServiceProvider();

        var options = new SwapEventBusOptions();
        options.When(SseEvents.Broadcast("stats-update"))
            .RefreshPartial("dashboard-stats", "~/Views/Dashboard/Stats.cshtml", _ => new { Total = 42 });

        var logger = new Mock<ILogger<SseEventBridge>>().Object;

        var bridge = new SseEventBridge(
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
        var mockRegistry = new Mock<ISseConnectionRegistry>();
        var mockViewRenderer = new Mock<ISseViewRenderer>();
        mockViewRenderer.Setup(r => r.RenderPartialAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<div>Room Content</div>");

        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        var serviceProvider = services.BuildServiceProvider();

        var options = new SwapEventBusOptions();
        options.When(SseEvents.Rooms("team-alpha").Send("team-update"))
            .RefreshPartial("team-info", "~/Views/Teams/Info.cshtml", _ => new { Team = "alpha" });

        var logger = new Mock<ILogger<SseEventBridge>>().Object;

        var bridge = new SseEventBridge(
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
        var mockRegistry = new Mock<ISseConnectionRegistry>();
        var mockViewRenderer = new Mock<ISseViewRenderer>();
        mockViewRenderer.Setup(r => r.RenderPartialAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<div>User Content</div>");

        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        var serviceProvider = services.BuildServiceProvider();

        var options = new SwapEventBusOptions();
        options.When(SseEvents.User("notification", "user123"))
            .RefreshPartial("notification-badge", "~/Views/Notifications/Badge.cshtml", _ => new { Count = 5 });

        var logger = new Mock<ILogger<SseEventBridge>>().Object;

        var bridge = new SseEventBridge(
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
        var mockRegistry = new Mock<ISseConnectionRegistry>();
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

        var logger = new Mock<ILogger<SseEventBridge>>().Object;

        var bridge = new SseEventBridge(
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
        var mockRegistry = new Mock<ISseConnectionRegistry>();
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

        var logger = new Mock<ILogger<SseEventBridge>>().Object;

        var bridge = new SseEventBridge(
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
        var mockRegistry = new Mock<ISseConnectionRegistry>();
        var mockViewRenderer = new Mock<ISseViewRenderer>();
        mockViewRenderer.Setup(r => r.RenderPartialAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<div>Admin Content</div>");

        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        var serviceProvider = services.BuildServiceProvider();

        var options = new SwapEventBusOptions();
        options.When(SseEvents.Roles("system-alert", "Admin"))
            .RefreshPartial("alert-box", "~/Views/Shared/Alert.cshtml", _ => new { Message = "Alert" });

        var logger = new Mock<ILogger<SseEventBridge>>().Object;

        var bridge = new SseEventBridge(
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
}

public class TaskPayload
{
    public string TaskId { get; set; } = string.Empty;
}

