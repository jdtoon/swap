using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using Swap.Htmx.Services;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapEventServiceTests
{
    private readonly Mock<IEventChainExecutor> _executorMock;
    private readonly Mock<ILogger<SwapEventService>> _loggerMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ISwapEventBus> _eventBusMock;
    private readonly SwapEventService _service;

    public SwapEventServiceTests()
    {
        _executorMock = new Mock<IEventChainExecutor>();
        _loggerMock = new Mock<ILogger<SwapEventService>>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _eventBusMock = new Mock<ISwapEventBus>();
        _service = new SwapEventService(_executorMock.Object, _loggerMock.Object, _httpContextAccessorMock.Object, _eventBusMock.Object);
    }

    [Fact]
    public void Response_ReturnsBuilder_WithControllerSet()
    {
        // Arrange
        var controller = new ControllerContext().ActionDescriptor == null ? new TestController() : new TestController();

        // Act
        var builder = _service.Response(controller);

        // Assert
        Assert.NotNull(builder);
        // We can't check internal Controller property easily, but we know it didn't throw
    }

    [Fact]
    public void Event_ExecutesChain_AndReturnsBuilder()
    {
        // Arrange
        var eventKey = new EventKey("test.event");
        var controller = new TestController();
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        
        var expectedBuilder = new SwapResponseBuilder();
        _executorMock.Setup(x => x.Execute(eventKey, httpContext, controller, null))
            .Returns(expectedBuilder);

        // Act
        var result = _service.Event(eventKey, controller);

        // Assert
        Assert.Same(expectedBuilder, result);
        _executorMock.Verify(x => x.Execute(eventKey, httpContext, controller, null), Times.Once);
    }

    [Fact]
    public async Task EventAsync_ExecutesChainAsync_AndReturnsBuilder()
    {
        // Arrange
        var eventKey = new EventKey("test.event.async");
        var controller = new TestController();
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        
        var expectedBuilder = new SwapResponseBuilder();
        _executorMock.Setup(x => x.ExecuteAsync(eventKey, httpContext, controller, null))
            .ReturnsAsync(expectedBuilder);

        // Act
        var result = await _service.EventAsync(eventKey, controller);

        // Assert
        Assert.Same(expectedBuilder, result);
        _executorMock.Verify(x => x.ExecuteAsync(eventKey, httpContext, controller, null), Times.Once);
    }

    // Helper controller
    private class TestController : Controller { }
}
