using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Swap.Htmx.Extensions;
using Swap.Htmx.Models;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapOobExtensionsTests
{
    [Theory]
    [InlineData("order-status", 42, "order-status-42")]
    [InlineData("product-card", 123, "product-card-123")]
    [InlineData("cart-item", 0, "cart-item-0")]
    [InlineData("notification", "abc-def", "notification-abc-def")]
    public void WithId_CombinesBaseIdAndInstanceId(string baseId, object instanceId, string expected)
    {
        // Act
        var result = baseId.WithId(instanceId);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void WithId_WithComplexObject_UsesToString()
    {
        // Arrange
        var obj = new { Id = 999 };

        // Act
        var result = "item".WithId(obj);

        // Assert
        Assert.Contains("item-", result);
    }

    [Fact]
    public void AlsoUpdateById_AddsOobSwapWithCombinedId()
    {
        // Arrange
        var controller = new TestController();
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        var builder = new SwapResponseBuilder { Controller = controller };
        var model = new { Status = "Shipped" };

        // Act
        var result = builder.AlsoUpdateById("order-status", 42, "_OrderStatus", model);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.OobSwaps);
        Assert.Equal("order-status-42", result.OobSwaps[0].TargetId);
        Assert.Equal("_OrderStatus", result.OobSwaps[0].ViewName);
        Assert.Same(model, result.OobSwaps[0].Model);
    }

    [Fact]
    public void AlsoUpdateById_WithCustomSwapMode_UsesSpecifiedMode()
    {
        // Arrange
        var controller = new TestController();
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        var builder = new SwapResponseBuilder { Controller = controller };

        // Act
        var result = builder.AlsoUpdateById("container", 5, "_Content", null, SwapMode.InnerHTML);

        // Assert
        Assert.Equal(SwapMode.InnerHTML, result.OobSwaps[0].SwapMode);
    }

    [Fact]
    public void AlsoUpdateById_SupportsChaining()
    {
        // Arrange
        var controller = new TestController();
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        var builder = new SwapResponseBuilder { Controller = controller };

        // Act
        var result = builder
            .AlsoUpdateById("order-status", 1, "_Status")
            .AlsoUpdateById("order-total", 1, "_Total")
            .AlsoUpdateById("order-items", 1, "_Items");

        // Assert
        Assert.Equal(3, result.OobSwaps.Count);
        Assert.Equal("order-status-1", result.OobSwaps[0].TargetId);
        Assert.Equal("order-total-1", result.OobSwaps[1].TargetId);
        Assert.Equal("order-items-1", result.OobSwaps[2].TargetId);
    }

    private class TestController : Controller
    {
    }
}
