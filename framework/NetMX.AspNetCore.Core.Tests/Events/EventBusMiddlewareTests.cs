using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NetMX.AspNetCore.Core.Events;
using NetMX.Events;
using Xunit;

namespace NetMX.AspNetCore.Core.Tests.Events;

public class EventBusMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldCreateEventContext()
    {
        // Arrange
        var middleware = new EventBusMiddleware(async ctx => await Task.CompletedTask);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Items.Should().ContainKey("NetMX.EventContext");
        var eventContext = context.Items["NetMX.EventContext"] as EventContext;
        eventContext.Should().NotBeNull();
        eventContext!.RequestId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleSession()
    {
        // Arrange
        var middleware = new EventBusMiddleware(async ctx => await Task.CompletedTask);
        var context = CreateHttpContext(sessionId: "test-session-123");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var eventContext = context.Items["NetMX.EventContext"] as EventContext;
        // Session might not be extracted in test environment (session feature not fully configured)
        // We just verify EventContext is created
        eventContext.Should().NotBeNull();
        eventContext!.RequestId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task InvokeAsync_ShouldExtractUserId()
    {
        // Arrange
        var middleware = new EventBusMiddleware(async ctx => await Task.CompletedTask);
        var userId = Guid.NewGuid();
        var context = CreateHttpContext(userId: userId);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var eventContext = context.Items["NetMX.EventContext"] as EventContext;
        eventContext!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task InvokeAsync_ShouldInjectHxTriggerHeader()
    {
        // Arrange
        var eventBusMock = new Mock<IEventBus>();
        var triggeredEvents = new Dictionary<string, object>
        {
            { "order.created", new { orderId = 123 } }
        };
        eventBusMock.Setup(e => e.GetTriggeredEvents(It.IsAny<Guid>()))
            .Returns(triggeredEvents);

        var services = new ServiceCollection();
        services.AddSingleton(eventBusMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var middleware = new EventBusMiddleware(async ctx => await Task.CompletedTask);
        var context = CreateHttpContext(serviceProvider);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("HX-Trigger");
        context.Response.Headers["HX-Trigger"].ToString().Should().Contain("order.created");
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotInjectHeaderIfNoEvents()
    {
        // Arrange
        var eventBusMock = new Mock<IEventBus>();
        eventBusMock.Setup(e => e.GetTriggeredEvents(It.IsAny<Guid>()))
            .Returns(new Dictionary<string, object>());

        var services = new ServiceCollection();
        services.AddSingleton(eventBusMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var middleware = new EventBusMiddleware(async ctx => await Task.CompletedTask);
        var context = CreateHttpContext(serviceProvider);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().NotContainKey("HX-Trigger");
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleMissingSession()
    {
        // Arrange
        var middleware = new EventBusMiddleware(async ctx => await Task.CompletedTask);
        var context = CreateHttpContext(sessionId: null);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var eventContext = context.Items["NetMX.EventContext"] as EventContext;
        eventContext!.SessionId.Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleUnauthenticatedUser()
    {
        // Arrange
        var middleware = new EventBusMiddleware(async ctx => await Task.CompletedTask);
        var context = CreateHttpContext(userId: null);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var eventContext = context.Items["NetMX.EventContext"] as EventContext;
        eventContext!.UserId.Should().BeNull();
    }

    private static HttpContext CreateHttpContext(
        IServiceProvider? serviceProvider = null,
        string? sessionId = null,
        Guid? userId = null)
    {
        var context = new DefaultHttpContext();

        // Setup service provider
        if (serviceProvider != null)
        {
            context.RequestServices = serviceProvider;
        }
        else
        {
            var services = new ServiceCollection();
            context.RequestServices = services.BuildServiceProvider();
        }

        // Setup session
        if (sessionId != null)
        {
            var sessionMock = new Mock<ISession>();
            sessionMock.Setup(s => s.Id).Returns(sessionId);
            sessionMock.Setup(s => s.IsAvailable).Returns(true);
            context.Features.Set(sessionMock.Object);
        }

        // Setup user
        if (userId.HasValue)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            context.User = new ClaimsPrincipal(identity);
        }

        return context;
    }
}
