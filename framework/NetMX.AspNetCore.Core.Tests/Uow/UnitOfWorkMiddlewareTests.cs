using Microsoft.AspNetCore.Http;
using Moq;
using NetMX.AspNetCore.Core.Uow;
using NetMX.Ddd.Application.Uow;

namespace NetMX.AspNetCore.Core.Tests.Uow;

public class UnitOfWorkMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_SuccessfulRequest_CompletesUnitOfWork()
    {
        // Arrange
        var mockUowManager = new Mock<IUnitOfWorkManager>();
        var mockUow = new Mock<IUnitOfWork>();
        mockUowManager.Setup(m => m.Begin(It.IsAny<bool>(), It.IsAny<bool>())).Returns(mockUow.Object);
        mockUowManager.Setup(m => m.Current).Returns((IUnitOfWork?)null);

        var context = new DefaultHttpContext();
        context.Response.StatusCode = 200;

        var nextCalled = false;
        Task Next(HttpContext ctx)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        var middleware = new UnitOfWorkMiddleware(Next);

        // Act
        await middleware.InvokeAsync(context, mockUowManager.Object);

        // Assert
        Assert.True(nextCalled);
        mockUowManager.Verify(m => m.Begin(false, true), Times.Once);
        mockUow.Verify(m => m.CompleteAsync(default), Times.Once);
        mockUow.Verify(m => m.Dispose(), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ErrorStatusCode_DoesNotCompleteUnitOfWork()
    {
        // Arrange
        var mockUowManager = new Mock<IUnitOfWorkManager>();
        var mockUow = new Mock<IUnitOfWork>();
        mockUowManager.Setup(m => m.Begin(It.IsAny<bool>(), It.IsAny<bool>())).Returns(mockUow.Object);
        mockUowManager.Setup(m => m.Current).Returns((IUnitOfWork?)null);

        var context = new DefaultHttpContext();
        context.Response.StatusCode = 500; // Server error

        Task Next(HttpContext ctx) => Task.CompletedTask;

        var middleware = new UnitOfWorkMiddleware(Next);

        // Act
        await middleware.InvokeAsync(context, mockUowManager.Object);

        // Assert
        mockUow.Verify(m => m.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        mockUow.Verify(m => m.Dispose(), Times.Once); // Still disposed (rollback)
    }

    [Fact]
    public async Task InvokeAsync_ExceptionThrown_DisposesUnitOfWorkWithoutCommit()
    {
        // Arrange
        var mockUowManager = new Mock<IUnitOfWorkManager>();
        var mockUow = new Mock<IUnitOfWork>();
        mockUowManager.Setup(m => m.Begin(It.IsAny<bool>(), It.IsAny<bool>())).Returns(mockUow.Object);
        mockUowManager.Setup(m => m.Current).Returns((IUnitOfWork?)null);

        var context = new DefaultHttpContext();

        Task Next(HttpContext ctx) => throw new InvalidOperationException("Test exception");

        var middleware = new UnitOfWorkMiddleware(Next);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.InvokeAsync(context, mockUowManager.Object));

        mockUow.Verify(m => m.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        mockUow.Verify(m => m.Dispose(), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_UnitOfWorkAlreadyActive_SkipsCreatingNew()
    {
        // Arrange
        var mockUowManager = new Mock<IUnitOfWorkManager>();
        var existingUow = new Mock<IUnitOfWork>();
        mockUowManager.Setup(m => m.Current).Returns(existingUow.Object); // UoW already active

        var context = new DefaultHttpContext();
        var nextCalled = false;
        Task Next(HttpContext ctx)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        var middleware = new UnitOfWorkMiddleware(Next);

        // Act
        await middleware.InvokeAsync(context, mockUowManager.Object);

        // Assert
        Assert.True(nextCalled);
        mockUowManager.Verify(m => m.Begin(It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_StatusCode400_DoesNotComplete()
    {
        // Arrange
        var mockUowManager = new Mock<IUnitOfWorkManager>();
        var mockUow = new Mock<IUnitOfWork>();
        mockUowManager.Setup(m => m.Begin(It.IsAny<bool>(), It.IsAny<bool>())).Returns(mockUow.Object);
        mockUowManager.Setup(m => m.Current).Returns((IUnitOfWork?)null);

        var context = new DefaultHttpContext();
        context.Response.StatusCode = 400; // Bad Request

        Task Next(HttpContext ctx) => Task.CompletedTask;

        var middleware = new UnitOfWorkMiddleware(Next);

        // Act
        await middleware.InvokeAsync(context, mockUowManager.Object);

        // Assert
        mockUow.Verify(m => m.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        mockUow.Verify(m => m.Dispose(), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_StatusCode399_Completes()
    {
        // Arrange
        var mockUowManager = new Mock<IUnitOfWorkManager>();
        var mockUow = new Mock<IUnitOfWork>();
        mockUowManager.Setup(m => m.Begin(It.IsAny<bool>(), It.IsAny<bool>())).Returns(mockUow.Object);
        mockUowManager.Setup(m => m.Current).Returns((IUnitOfWork?)null);

        var context = new DefaultHttpContext();
        context.Response.StatusCode = 399; // Edge case: just below 400

        Task Next(HttpContext ctx) => Task.CompletedTask;

        var middleware = new UnitOfWorkMiddleware(Next);

        // Act
        await middleware.InvokeAsync(context, mockUowManager.Object);

        // Assert
        mockUow.Verify(m => m.CompleteAsync(default), Times.Once);
    }
}
