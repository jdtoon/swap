using System;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NetMX.AspNetCore.Events;
using NetMX.Events;
using Xunit;

namespace NetMX.AspNetCore.Core.Tests.Events;

public class EventBusHttpContextExtensionsTests
{
    [Fact]
    public void GetEventContext_ShouldReturnStoredContext()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var eventContext = new EventContext
        {
            RequestId = Guid.NewGuid(),
            SessionId = "test-session",
            UserId = Guid.NewGuid()
        };
        httpContext.Items["NetMX.EventContext"] = eventContext;

        // Act
        var result = httpContext.GetEventContext();

        // Assert
        result.Should().BeSameAs(eventContext);
    }

    [Fact]
    public void GetEventContext_ShouldCreateFallbackIfNotFound()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        var result = httpContext.GetEventContext();

        // Assert
        result.Should().NotBeNull();
        result.RequestId.Should().NotBe(Guid.Empty);
        result.SessionId.Should().BeEmpty();
        result.UserId.Should().BeNull();
    }

    [Fact]
    public void HasEventContext_ShouldReturnTrueIfExists()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items["NetMX.EventContext"] = new EventContext();

        // Act
        var result = httpContext.HasEventContext();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasEventContext_ShouldReturnFalseIfNotExists()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        var result = httpContext.HasEventContext();

        // Assert
        result.Should().BeFalse();
    }
}
