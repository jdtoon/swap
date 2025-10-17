using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetMX.AspNetCore.Mvc.Htmx;
using Xunit;

namespace NetMX.AspNetCore.Mvc.Tests.Htmx;

public class HtmxLocationTests
{
    [Fact]
    public void HtmxLocation_SimpleRedirect_CreatesCorrectJson()
    {
        // Arrange
        var location = new HtmxLocation("/users/123");

        // Act
        var json = location.ToJson();

        // Assert
        Assert.Contains("\"path\":\"/users/123\"", json);
    }

    [Fact]
    public void HtmxLocation_WithAllOptions_CreatesCorrectJson()
    {
        // Arrange
        var location = new HtmxLocation("/users/123")
            .WithSource("#user-form")
            .WithEvent("user-created")
            .WithTarget("#user-list")
            .WithSwap(HtmxSwap.OuterHTML)
            .WithValues(new { id = 123, name = "John" })
            .WithHeader("X-Custom", "value");

        // Act
        var json = location.ToJson();

        // Assert
        Assert.Contains("\"path\":\"/users/123\"", json);
        Assert.Contains("\"source\":\"#user-form\"", json);
        Assert.Contains("\"event\":\"user-created\"", json);
        Assert.Contains("\"target\":\"#user-list\"", json);
        Assert.Contains("\"swap\":\"outerHTML\"", json);
        Assert.Contains("\"values\":", json);
        Assert.Contains("\"headers\":", json);
    }

    [Fact]
    public void HxLocation_SetsCorrectHeader()
    {
        // Arrange
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        var location = new HtmxLocation("/users/123");

        // Act
        controller.HxLocation(location);

        // Assert
        Assert.True(controller.Response.Headers.ContainsKey("HX-Location"));
        var headerValue = controller.Response.Headers["HX-Location"].ToString();
        Assert.Contains("\"/users/123\"", headerValue);
    }
}

public class HtmxSwapModifierTests
{
    [Fact]
    public void Build_WithNoModifiers_ReturnsSwapOnly()
    {
        // Arrange & Act
        var result = HtmxSwapModifier.Build(HtmxSwap.InnerHTML);

        // Assert
        Assert.Equal(HtmxSwap.InnerHTML, result);
    }

    [Fact]
    public void Build_WithModifiers_CombinesCorrectly()
    {
        // Arrange & Act
        var result = HtmxSwapModifier.Build(
            HtmxSwap.InnerHTML,
            HtmxSwapModifier.ScrollTop(),
            HtmxSwapModifier.ShowNone());

        // Assert
        Assert.Equal("innerHTML scroll:top show:none", result);
    }

    [Fact]
    public void ScrollModifiers_GenerateCorrectStrings()
    {
        Assert.Equal("scroll:top", HtmxSwapModifier.ScrollTop());
        Assert.Equal("scroll:bottom", HtmxSwapModifier.ScrollBottom());
        Assert.Equal("scroll:#element:top", HtmxSwapModifier.Scroll("#element"));
        Assert.Equal("scroll:#element:bottom", HtmxSwapModifier.ScrollBottom("#element"));
    }

    [Fact]
    public void ShowModifiers_GenerateCorrectStrings()
    {
        Assert.Equal("show:top", HtmxSwapModifier.ShowTop());
        Assert.Equal("show:bottom", HtmxSwapModifier.ShowBottom());
        Assert.Equal("show:#element:top", HtmxSwapModifier.Show("#element"));
        Assert.Equal("show:#element:bottom", HtmxSwapModifier.ShowBottom("#element"));
        Assert.Equal("show:none", HtmxSwapModifier.ShowNone());
    }

    [Fact]
    public void FocusScrollModifiers_GenerateCorrectStrings()
    {
        Assert.Equal("focus-scroll:true", HtmxSwapModifier.FocusScrollTrue());
        Assert.Equal("focus-scroll:false", HtmxSwapModifier.FocusScrollFalse());
    }

    [Fact]
    public void TimingModifiers_GenerateCorrectStrings()
    {
        Assert.Equal("swap:500ms", HtmxSwapModifier.Swap("500ms"));
        Assert.Equal("settle:1s", HtmxSwapModifier.Settle("1s"));
    }

    [Fact]
    public void TransitionModifiers_GenerateCorrectStrings()
    {
        Assert.Equal("transition:true", HtmxSwapModifier.TransitionTrue());
        Assert.Equal("transition:false", HtmxSwapModifier.TransitionFalse());
    }

    [Fact]
    public void IgnoreTitle_GeneratesCorrectString()
    {
        Assert.Equal("ignoreTitle:true", HtmxSwapModifier.IgnoreTitle());
    }
}

public class HtmxResponseExtensionsAdvancedTests
{
    [Fact]
    public void HxOutOfBandSwap_GeneratesCorrectHtml()
    {
        // Arrange
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Act
        var result = controller.HxOutOfBandSwap("Updated content", "user-row-123");

        // Assert
        Assert.Equal("text/html", result.ContentType);
        Assert.Contains("id=\"user-row-123\"", result.Content);
        Assert.Contains("hx-swap-oob=\"innerHTML\"", result.Content);
        Assert.Contains("Updated content", result.Content);
    }

    [Fact]
    public void HxOutOfBandSwaps_GeneratesMultipleSwaps()
    {
        // Arrange
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Act
        var result = controller.HxOutOfBandSwaps(
            ("stats", "10 users", HtmxSwap.InnerHTML),
            ("notifications", "3 new", HtmxSwap.InnerHTML));

        // Assert
        Assert.Contains("id=\"stats\"", result.Content);
        Assert.Contains("10 users", result.Content);
        Assert.Contains("id=\"notifications\"", result.Content);
        Assert.Contains("3 new", result.Content);
    }

    [Fact]
    public void HxPoll_SetsCorrectTriggerHeader()
    {
        // Arrange
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Act
        controller.HxPoll(5000);

        // Assert
        Assert.True(controller.Response.Headers.ContainsKey("HX-Trigger"));
        var headerValue = controller.Response.Headers["HX-Trigger"].ToString();
        Assert.Contains("load", headerValue);
        Assert.Contains("5000", headerValue);
    }

    [Fact]
    public void HxStopPoll_TriggersStopEvent()
    {
        // Arrange
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Act
        controller.HxStopPoll();

        // Assert
        Assert.True(controller.Response.Headers.ContainsKey("HX-Trigger"));
        Assert.Equal("stop-polling", controller.Response.Headers["HX-Trigger"]);
    }

    [Fact]
    public void HxStopPollingResponse_Returns286()
    {
        // Arrange
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Act
        var result = controller.HxStopPollingResponse();

        // Assert
        Assert.Equal(286, result.StatusCode);
    }

    [Fact]
    public void HxScrollTop_SetsCorrectReswap()
    {
        // Arrange
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Act
        controller.HxScrollTop();

        // Assert
        Assert.True(controller.Response.Headers.ContainsKey("HX-Reswap"));
        var headerValue = controller.Response.Headers["HX-Reswap"].ToString();
        Assert.Contains("scroll:top", headerValue);
    }

    [Fact]
    public void HxScrollBottom_SetsCorrectReswap()
    {
        // Arrange
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Act
        controller.HxScrollBottom();

        // Assert
        Assert.True(controller.Response.Headers.ContainsKey("HX-Reswap"));
        var headerValue = controller.Response.Headers["HX-Reswap"].ToString();
        Assert.Contains("scroll:bottom", headerValue);
    }

    [Fact]
    public void HxScrollTo_SetsCorrectReswap()
    {
        // Arrange
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Act
        controller.HxScrollTo("#target");

        // Assert
        Assert.True(controller.Response.Headers.ContainsKey("HX-Reswap"));
        var headerValue = controller.Response.Headers["HX-Reswap"].ToString();
        Assert.Contains("scroll:#target:top", headerValue);
    }

    [Fact]
    public void HxReswapWithModifiers_CombinesCorrectly()
    {
        // Arrange
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Act
        controller.HxReswapWithModifiers(
            HtmxSwap.OuterHTML,
            HtmxSwapModifier.ScrollTop(),
            HtmxSwapModifier.TransitionTrue());

        // Assert
        Assert.True(controller.Response.Headers.ContainsKey("HX-Reswap"));
        var headerValue = controller.Response.Headers["HX-Reswap"].ToString();
        Assert.Equal("outerHTML scroll:top transition:true", headerValue);
    }
}

// Test controller helper
public class TestController : Controller
{
}
