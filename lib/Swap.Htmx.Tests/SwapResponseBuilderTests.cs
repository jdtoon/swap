using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Swap.Htmx.Extensions;
using Swap.Htmx.Models;
using Xunit;

namespace Swap.Htmx.Tests;

/// <summary>
/// Tests for the SwapResponseBuilder fluent API.
/// </summary>
public class SwapResponseBuilderTests
{
    private class TestSwapController : SwapController
    {
        public SwapResponseBuilder TestSwapResponse() => SwapResponse();
        
        public ActionResult TestSimpleResponse() =>
            SwapResponse()
                .WithView("TestView");
        
        public ActionResult TestWithModel() =>
            SwapResponse()
                .WithView("TestView", new { Id = 123 });
        
        public ActionResult TestWithOob() =>
            SwapResponse()
                .WithView("Main")
                .AlsoUpdate("counter", "_Counter", 5);
        
        public ActionResult TestWithMultipleOob() =>
            SwapResponse()
                .WithView("Main")
                .AlsoUpdate("counter", "_Counter", 5)
                .AlsoUpdate("status", "_Status", "Active")
                .AlsoUpdate("total", "_Total", 100.50m);
        
        public ActionResult TestWithToast() =>
            SwapResponse()
                .WithView("Main")
                .WithSuccessToast("Operation successful!");
        
        public ActionResult TestWithAllToastTypes() =>
            SwapResponse()
                .WithView("Main")
                .WithSuccessToast("Success!")
                .WithErrorToast("Error!")
                .WithWarningToast("Warning!")
                .WithInfoToast("Info!");
        
        public ActionResult TestWithTrigger() =>
            SwapResponse()
                .WithView("Main")
                .WithTrigger("customEvent");
        
        public ActionResult TestWithTriggerAndPayload() =>
            SwapResponse()
                .WithView("Main")
                .WithTrigger("customEvent", new { id = 123, status = "completed" });
        
        public ActionResult TestCompleteScenario() =>
            SwapResponse()
                .WithView("_ProductAdded")
                .AlsoUpdate("cart-count", "_CartCount", 3)
                .AlsoUpdate("cart-total", "_CartTotal", 99.99m)
                .WithSuccessToast("Product added to cart!")
                .WithTrigger("cart.updated", new { itemCount = 3 });
        
        public ActionResult TestSwapModes() =>
            SwapResponse()
                .WithView("Main")
                .AlsoUpdate("list", "_List", null, SwapMode.InnerHTML)
                .AlsoUpdate("notifications", "_Notification", null, SwapMode.BeforeEnd)
                .AlsoUpdate("old-item", "_Empty", null, SwapMode.Delete);
    }

    private static TestSwapController CreateController()
    {
        var controller = new TestSwapController();
        
        // Set up minimal ControllerContext
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        // Initialize ViewData and TempData
        controller.ViewData = new ViewDataDictionary(
            new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
            new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());
        
        controller.TempData = new TempDataDictionary(
            httpContext,
            new SessionStateTempDataProvider());
        
        return controller;
    }

    [Fact]
    public void SwapResponse_CreatesBuilder()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var builder = controller.TestSwapResponse();

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<SwapResponseBuilder>(builder);
    }

    [Fact]
    public void WithView_SetsViewName()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.TestSwapResponse();

        // Act
        builder.WithView("MyView");

        // Assert
        Assert.Equal("MyView", builder.ViewName);
    }

    [Fact]
    public void WithView_SetsModel()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.TestSwapResponse();
        var model = new { Name = "Test" };

        // Act
        builder.WithView("MyView", model);

        // Assert
        Assert.Same(model, builder.Model);
    }

    [Fact]
    public void AlsoUpdate_AddsOobSwap()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.TestSwapResponse();

        // Act
        builder.AlsoUpdate("counter", "_Counter", 5);

        // Assert
        Assert.Single(builder.OobSwaps);
        var oob = builder.OobSwaps[0];
        Assert.Equal("counter", oob.TargetId);
        Assert.Equal("_Counter", oob.ViewName);
        Assert.Equal(5, oob.Model);
        Assert.Equal(SwapMode.OuterHTML, oob.SwapMode);
    }

    [Fact]
    public void AlsoUpdate_SupportsMultipleOobs()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.TestSwapResponse();

        // Act
        builder
            .AlsoUpdate("counter", "_Counter", 5)
            .AlsoUpdate("status", "_Status", "Active")
            .AlsoUpdate("total", "_Total", 100.50m);

        // Assert
        Assert.Equal(3, builder.OobSwaps.Count);
        Assert.Equal("counter", builder.OobSwaps[0].TargetId);
        Assert.Equal("status", builder.OobSwaps[1].TargetId);
        Assert.Equal("total", builder.OobSwaps[2].TargetId);
    }

    [Fact]
    public void AlsoUpdate_SupportsCustomSwapMode()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.TestSwapResponse();

        // Act
        builder.AlsoUpdate("list", "_List", null, SwapMode.InnerHTML);

        // Assert
        var oob = builder.OobSwaps[0];
        Assert.Equal(SwapMode.InnerHTML, oob.SwapMode);
    }

    [Fact]
    public void WithToast_AddsToastNotification()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.TestSwapResponse();

        // Act
        builder.WithToast("Test message", ToastType.Success);

        // Assert
        Assert.Single(builder.Toasts);
        var toast = builder.Toasts[0];
        Assert.Equal("Test message", toast.Message);
        Assert.Equal(ToastType.Success, toast.Type);
    }

    [Fact]
    public void WithSuccessToast_AddsSuccessToast()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.TestSwapResponse();

        // Act
        builder.WithSuccessToast("Success!");

        // Assert
        var toast = builder.Toasts[0];
        Assert.Equal(ToastType.Success, toast.Type);
        Assert.Equal("Success!", toast.Message);
    }

    [Fact]
    public void WithErrorToast_AddsErrorToast()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.TestSwapResponse();

        // Act
        builder.WithErrorToast("Error!");

        // Assert
        var toast = builder.Toasts[0];
        Assert.Equal(ToastType.Error, toast.Type);
    }

    [Fact]
    public void WithWarningToast_AddsWarningToast()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.TestSwapResponse();

        // Act
        builder.WithWarningToast("Warning!");

        // Assert
        var toast = builder.Toasts[0];
        Assert.Equal(ToastType.Warning, toast.Type);
    }

    [Fact]
    public void WithInfoToast_AddsInfoToast()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.TestSwapResponse();

        // Act
        builder.WithInfoToast("Info!");

        // Assert
        var toast = builder.Toasts[0];
        Assert.Equal(ToastType.Info, toast.Type);
    }

    [Fact]
    public void WithToast_SupportsMultipleToasts()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.TestSwapResponse();

        // Act
        builder
            .WithSuccessToast("Success!")
            .WithErrorToast("Error!")
            .WithWarningToast("Warning!");

        // Assert
        Assert.Equal(3, builder.Toasts.Count);
    }

    [Fact]
    public void WithTrigger_AddsTriggerEvent()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.TestSwapResponse();

        // Act
        builder.WithTrigger("customEvent");

        // Assert
        Assert.Single(builder.Triggers);
        var trigger = builder.Triggers[0];
        Assert.Equal("customEvent", trigger.EventName);
        Assert.Null(trigger.Payload);
    }

    [Fact]
    public void WithTrigger_SupportsPayload()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.TestSwapResponse();
        var payload = new { id = 123, status = "completed" };

        // Act
        builder.WithTrigger("customEvent", payload);

        // Assert
        var trigger = builder.Triggers[0];
        Assert.Equal("customEvent", trigger.EventName);
        Assert.Same(payload, trigger.Payload);
    }

    [Fact]
    public void WithTrigger_SupportsMultipleTriggers()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.TestSwapResponse();

        // Act
        builder
            .WithTrigger("event1")
            .WithTrigger("event2", new { data = "test" })
            .WithTrigger("event3");

        // Assert
        Assert.Equal(3, builder.Triggers.Count);
    }

    [Fact]
    public void FluentAPI_SupportsMethodChaining()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var builder = controller.TestSwapResponse()
            .WithView("Main")
            .AlsoUpdate("counter", "_Counter", 1)
            .WithSuccessToast("Done!")
            .WithTrigger("completed");

        // Assert
        Assert.NotNull(builder);
        Assert.Equal("Main", builder.ViewName);
        Assert.Single(builder.OobSwaps);
        Assert.Single(builder.Toasts);
        Assert.Single(builder.Triggers);
    }

    [Fact]
    public void CompleteScenario_ConfiguresAllFeatures()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var builder = controller.TestSwapResponse()
            .WithView("_ProductAdded")
            .AlsoUpdate("cart-count", "_CartCount", 3)
            .AlsoUpdate("cart-total", "_CartTotal", 99.99m)
            .WithSuccessToast("Product added to cart!")
            .WithTrigger("cart.updated", new { itemCount = 3 });

        // Assert
        Assert.Equal("_ProductAdded", builder.ViewName);
        Assert.Equal(2, builder.OobSwaps.Count);
        Assert.Single(builder.Toasts);
        Assert.Single(builder.Triggers);
        
        // Verify OOB swaps
        Assert.Equal("cart-count", builder.OobSwaps[0].TargetId);
        Assert.Equal("cart-total", builder.OobSwaps[1].TargetId);
        
        // Verify toast
        Assert.Equal("Product added to cart!", builder.Toasts[0].Message);
        Assert.Equal(ToastType.Success, builder.Toasts[0].Type);
        
        // Verify trigger
        Assert.Equal("cart.updated", builder.Triggers[0].EventName);
        Assert.NotNull(builder.Triggers[0].Payload);
    }

    [Fact]
    public void ImplicitConversion_ConvertsToActionResult()
    {
        // Arrange
        var controller = CreateController();

        // Act
        IActionResult result = controller.TestSimpleResponse();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Results.SwapActionResult>(result);
    }
}

/// <summary>
/// Minimal TempData provider for testing.
/// </summary>
internal class SessionStateTempDataProvider : ITempDataProvider
{
    private readonly Dictionary<string, object?> _data = new();

    public IDictionary<string, object?> LoadTempData(HttpContext context) => _data;

    public void SaveTempData(HttpContext context, IDictionary<string, object?> values)
    {
        _data.Clear();
        foreach (var kvp in values)
        {
            _data[kvp.Key] = kvp.Value;
        }
    }
}
