using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapControllerTests
{
    private class TestSwapController : SwapController
    {
        public IActionResult TestSwapView(object? model = null) => SwapView(model);
        public IActionResult TestSwapViewWithName(string? viewName, object? model = null) => SwapView(viewName, model);
    }

    private static TestSwapController CreateControllerWithRequest(bool includeHxRequestHeader = false)
    {
        var controller = new TestSwapController();
        var httpContext = new DefaultHttpContext();
        
        if (includeHxRequestHeader)
        {
            httpContext.Request.Headers["HX-Request"] = "true";
        }
        
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
}
