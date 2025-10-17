using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetMX.AspNetCore.Mvc.Htmx;
using System.Text.Json;

namespace NetMX.AspNetCore.Mvc.Tests.Htmx;

public class HtmxResponseExtensionsTests
{
    private TestController CreateController()
    {
        var controller = new TestController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    [Fact]
    public void HxRedirect_SetsCorrectHeader()
    {
        // Arrange
        var controller = CreateController();
        var url = "/new-page";

        // Act
        controller.HxRedirect(url);

        // Assert
        Assert.Equal(url, controller.Response.Headers["HX-Redirect"].ToString());
    }

    [Fact]
    public void HxRefresh_SetsCorrectHeader()
    {
        // Arrange
        var controller = CreateController();

        // Act
        controller.HxRefresh();

        // Assert
        Assert.Equal("true", controller.Response.Headers["HX-Refresh"].ToString());
    }

    [Fact]
    public void HxPushUrl_SetsCorrectHeader()
    {
        // Arrange
        var controller = CreateController();
        var url = "/pushed-url";

        // Act
        controller.HxPushUrl(url);

        // Assert
        Assert.Equal(url, controller.Response.Headers["HX-Push-Url"].ToString());
    }

    [Fact]
    public void HxReplaceUrl_SetsCorrectHeader()
    {
        // Arrange
        var controller = CreateController();
        var url = "/replaced-url";

        // Act
        controller.HxReplaceUrl(url);

        // Assert
        Assert.Equal(url, controller.Response.Headers["HX-Replace-Url"].ToString());
    }

    [Fact]
    public void HxReswap_SetsCorrectHeader()
    {
        // Arrange
        var controller = CreateController();
        var swapStyle = HtmxSwap.OuterHTML;

        // Act
        controller.HxReswap(swapStyle);

        // Assert
        Assert.Equal(swapStyle, controller.Response.Headers["HX-Reswap"].ToString());
    }

    [Fact]
    public void HxRetarget_SetsCorrectHeader()
    {
        // Arrange
        var controller = CreateController();
        var selector = "#new-target";

        // Act
        controller.HxRetarget(selector);

        // Assert
        Assert.Equal(selector, controller.Response.Headers["HX-Retarget"].ToString());
    }

    [Fact]
    public void HxReselect_SetsCorrectHeader()
    {
        // Arrange
        var controller = CreateController();
        var selector = ".content";

        // Act
        controller.HxReselect(selector);

        // Assert
        Assert.Equal(selector, controller.Response.Headers["HX-Reselect"].ToString());
    }

    [Fact]
    public void HxTrigger_WithEventName_SetsCorrectHeader()
    {
        // Arrange
        var controller = CreateController();
        var eventName = "myEvent";

        // Act
        controller.HxTrigger(eventName);

        // Assert
        Assert.Equal(eventName, controller.Response.Headers["HX-Trigger"].ToString());
    }

    [Fact]
    public void HxTrigger_WithEventNameAndData_SetsCorrectHeader()
    {
        // Arrange
        var controller = CreateController();
        var eventName = "dataEvent";
        var eventData = new { message = "Hello", count = 42 };

        // Act
        controller.HxTrigger(eventName, eventData);

        // Assert
        var headerValue = controller.Response.Headers["HX-Trigger"].ToString();
        var deserialized = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(headerValue);
        
        Assert.NotNull(deserialized);
        Assert.True(deserialized.ContainsKey(eventName));
    }

    [Fact]
    public void HxTrigger_WithMultipleEvents_SetsCorrectHeader()
    {
        // Arrange
        var controller = CreateController();
        var events = new[] { "event1", "event2", "event3" };

        // Act
        controller.HxTrigger(events);

        // Assert
        var headerValue = controller.Response.Headers["HX-Trigger"].ToString();
        Assert.Equal("event1, event2, event3", headerValue);
    }

    [Fact]
    public void HxTriggerAfterSettle_WithEventName_SetsCorrectHeader()
    {
        // Arrange
        var controller = CreateController();
        var eventName = "settleEvent";

        // Act
        controller.HxTriggerAfterSettle(eventName);

        // Assert
        Assert.Equal(eventName, controller.Response.Headers["HX-Trigger-After-Settle"].ToString());
    }

    [Fact]
    public void HxTriggerAfterSwap_WithEventName_SetsCorrectHeader()
    {
        // Arrange
        var controller = CreateController();
        var eventName = "swapEvent";

        // Act
        controller.HxTriggerAfterSwap(eventName);

        // Assert
        Assert.Equal(eventName, controller.Response.Headers["HX-Trigger-After-Swap"].ToString());
    }

    // Test controller
    private class TestController : Controller
    {
    }
}
