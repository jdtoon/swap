using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapControllerTests
{
    private class TestSwapController : SwapController
    {
        public IActionResult TestSwapView(object? model = null) => SwapView(model);
        public IActionResult TestSwapViewWithName(string? viewName, object? model = null) => SwapView(viewName, model);
        public IActionResult TestSwapOobView(string targetId, string? viewName = null, object? model = null, string swapStrategy = "true") 
            => SwapOobView(targetId, viewName, model, swapStrategy);
        public string TestGetOrInitializeSessionId() => GetOrInitializeSessionId();
    }

    private static TestSwapController CreateControllerWithRequest(bool includeHxRequestHeader = false)
    {
        var controller = new TestSwapController();
        var httpContext = new DefaultHttpContext();
        
        if (includeHxRequestHeader)
        {
            httpContext.Request.Headers["HX-Request"] = "true";
        }

        // Ensure RequestServices is not null to avoid ArgumentNullException in GetOrInitializeSessionId
        httpContext.RequestServices = new ServiceCollection().BuildServiceProvider();
        
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        // Mock TempData to avoid null reference
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        
        return controller;
    }

    [Fact]
    public void SwapView_WithHxRequestHeader_ReturnsPartialViewResult()
    {
        // Arrange
        var controller = CreateControllerWithRequest(includeHxRequestHeader: true);
        var model = new { Name = "Test" };

        // Act
        var result = controller.TestSwapView(model);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        Assert.Equal(model, partialViewResult.Model);
    }

    [Fact]
    public void SwapView_WithoutHxRequestHeader_ReturnsViewResult()
    {
        // Arrange
        var controller = CreateControllerWithRequest(includeHxRequestHeader: false);
        var model = new { Name = "Test" };

        // Act
        var result = controller.TestSwapView(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
    }

    [Fact]
    public void SwapView_WithViewNameAndHxRequest_ReturnsPartialViewWithName()
    {
        // Arrange
        var controller = CreateControllerWithRequest(includeHxRequestHeader: true);
        var model = new { Name = "Test" };
        var viewName = "CustomView";

        // Act
        var result = controller.TestSwapViewWithName(viewName, model);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        Assert.Equal(viewName, partialViewResult.ViewName);
        Assert.Equal(model, partialViewResult.Model);
    }

    [Fact]
    public void SwapView_WithViewNameAndNoHxRequest_ReturnsViewWithName()
    {
        // Arrange
        var controller = CreateControllerWithRequest(includeHxRequestHeader: false);
        var model = new { Name = "Test" };
        var viewName = "CustomView";

        // Act
        var result = controller.TestSwapViewWithName(viewName, model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewName, viewResult.ViewName);
        Assert.Equal(model, viewResult.Model);
    }

    [Fact]
    public void SwapView_WithNullModel_ReturnsResultWithNullModel()
    {
        // Arrange
        var controller = CreateControllerWithRequest(includeHxRequestHeader: true);

        // Act
        var result = controller.TestSwapView(null);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        Assert.Null(partialViewResult.Model);
    }

    [Fact]
    public void SwapView_WithNullViewName_UsesConventionalViewName()
    {
        // Arrange
        var controller = CreateControllerWithRequest(includeHxRequestHeader: false);

        // Act
        var result = controller.TestSwapViewWithName(null, new { Name = "Test" });

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.ViewName); // Null means use conventional name
    }

    [Fact]
    public void SwapOobView_ReturnsPartialViewResult()
    {
        // Arrange
        var controller = CreateControllerWithRequest();
        var model = new { Count = 5 };

        // Act
        var result = controller.TestSwapOobView("my-target", "MyView", model);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("MyView", partialViewResult.ViewName);
        Assert.Equal(model, partialViewResult.Model);
    }

    [Fact]
    public void SwapOobView_SetsViewDataWithOobAttributes()
    {
        // Arrange
        var controller = CreateControllerWithRequest();

        // Act
        var result = controller.TestSwapOobView("cart-total");

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("true", partialViewResult.ViewData["HxSwapOob"]);
        Assert.Equal("cart-total", partialViewResult.ViewData["OobTargetId"]);
    }

    [Fact]
    public void SwapOobView_WithCustomSwapStrategy_SetsCorrectViewData()
    {
        // Arrange
        var controller = CreateControllerWithRequest();

        // Act
        var result = controller.TestSwapOobView("notifications", swapStrategy: "beforeend");

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("beforeend", partialViewResult.ViewData["HxSwapOob"]);
        Assert.Equal("notifications", partialViewResult.ViewData["OobTargetId"]);
    }

    [Fact]
    public void SwapOobView_WithNullViewName_UsesConventionalName()
    {
        // Arrange
        var controller = CreateControllerWithRequest();

        // Act
        var result = controller.TestSwapOobView("target-id", viewName: null);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        Assert.Null(partialViewResult.ViewName);
    }

    [Fact]
    public void SwapOobView_WithNullModel_WorksCorrectly()
    {
        // Arrange
        var controller = CreateControllerWithRequest();

        // Act
        var result = controller.TestSwapOobView("target-id", model: null);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        Assert.Null(partialViewResult.Model);
        Assert.NotNull(partialViewResult.ViewData["HxSwapOob"]);
        Assert.NotNull(partialViewResult.ViewData["OobTargetId"]);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("innerHTML")]
    [InlineData("outerHTML")]
    [InlineData("beforebegin")]
    [InlineData("afterbegin")]
    [InlineData("beforeend")]
    [InlineData("afterend")]
    [InlineData("delete")]
    [InlineData("none")]
    public void SwapOobView_WithVariousSwapStrategies_SetsCorrectViewData(string strategy)
    {
        // Arrange
        var controller = CreateControllerWithRequest();

        // Act
        var result = controller.TestSwapOobView("target", swapStrategy: strategy);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        Assert.Equal(strategy, partialViewResult.ViewData["HxSwapOob"]);
    }

    [Fact]
    public void GetOrInitializeSessionId_FirstCall_InitializesSession()
    {
        // Arrange
        var controller = CreateControllerWithRequest();
        var session = new TestSession();
        controller.HttpContext!.Features.Set<ISessionFeature>(new TestSessionFeature(session));

        // Act
        var sessionId1 = controller.TestGetOrInitializeSessionId();
        var sessionId2 = controller.TestGetOrInitializeSessionId();

        // Assert
        Assert.Equal(sessionId1, sessionId2); // Should return same session ID
        Assert.True(session.ContainsKey("_swap_session_initialized"));
    }

    private class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        private readonly string _id = Guid.NewGuid().ToString();
        
        public string Id => _id;
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value)
        {
            if (_store.TryGetValue(key, out var val))
            {
                value = val;
                return true;
            }
            value = Array.Empty<byte>();
            return false;
        }
        public bool ContainsKey(string key) => _store.ContainsKey(key);
    }

    private class TestSessionFeature : ISessionFeature
    {
        public TestSessionFeature(ISession session) => Session = session;
        public ISession Session { get; set; }
    }
}

