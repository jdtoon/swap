using Microsoft.AspNetCore.Http;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapHtmxExtensionsTests
{
    #region Request Extension Tests

    [Fact]
    public void IsHtmxRequest_WithHxRequestHeader_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-Request"] = "true";

        // Act
        var result = context.Request.IsHtmxRequest();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHtmxRequest_WithoutHxRequestHeader_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var result = context.Request.IsHtmxRequest();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHtmxBoosted_WithHxBoostedHeader_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-Boosted"] = "true";

        // Act
        var result = context.Request.IsHtmxBoosted();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHtmxBoosted_WithoutHxBoostedHeader_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var result = context.Request.IsHtmxBoosted();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetHtmxCurrentUrl_WithHeader_ReturnsUrl()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var expectedUrl = "https://example.com/page";
        context.Request.Headers["HX-Current-URL"] = expectedUrl;

        // Act
        var result = context.Request.GetHtmxCurrentUrl();

        // Assert
        Assert.Equal(expectedUrl, result);
    }

    [Fact]
    public void GetHtmxCurrentUrl_WithoutHeader_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var result = context.Request.GetHtmxCurrentUrl();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetHtmxTarget_WithHeader_ReturnsTargetId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var expectedTarget = "content-div";
        context.Request.Headers["HX-Target"] = expectedTarget;

        // Act
        var result = context.Request.GetHtmxTarget();

        // Assert
        Assert.Equal(expectedTarget, result);
    }

    [Fact]
    public void GetHtmxTarget_WithoutHeader_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var result = context.Request.GetHtmxTarget();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetHtmxTrigger_WithHeader_ReturnsTriggerId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var expectedTrigger = "submit-button";
        context.Request.Headers["HX-Trigger"] = expectedTrigger;

        // Act
        var result = context.Request.GetHtmxTrigger();

        // Assert
        Assert.Equal(expectedTrigger, result);
    }

    [Fact]
    public void GetHtmxTrigger_WithoutHeader_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var result = context.Request.GetHtmxTrigger();

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Response Extension Tests

    [Fact]
    public void HxTrigger_SetsResponseHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var eventName = "itemCreated";

        // Act
        context.Response.HxTrigger(eventName);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("HX-Trigger"));
        Assert.Equal(eventName, context.Response.Headers["HX-Trigger"]);
    }

    [Fact]
    public void HxTriggerWithDetails_SetsResponseHeaderWithJson()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var json = "{\"showMessage\": {\"level\": \"info\", \"message\": \"Item saved\"}}";

        // Act
        context.Response.HxTriggerWithDetails(json);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("HX-Trigger"));
        Assert.Equal(json, context.Response.Headers["HX-Trigger"]);
    }

    [Fact]
    public void HxPushUrl_SetsResponseHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var url = "/articles/123";

        // Act
        context.Response.HxPushUrl(url);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("HX-Push-Url"));
        Assert.Equal(url, context.Response.Headers["HX-Push-Url"]);
    }

    [Fact]
    public void HxPreventPushUrl_SetsResponseHeaderToFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        context.Response.HxPreventPushUrl();

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("HX-Push-Url"));
        Assert.Equal("false", context.Response.Headers["HX-Push-Url"]);
    }

    [Fact]
    public void HxReplaceUrl_SetsResponseHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var url = "/articles/updated";

        // Act
        context.Response.HxReplaceUrl(url);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("HX-Replace-Url"));
        Assert.Equal(url, context.Response.Headers["HX-Replace-Url"]);
    }

    [Fact]
    public void HxRedirect_SetsResponseHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var url = "/login";

        // Act
        context.Response.HxRedirect(url);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("HX-Redirect"));
        Assert.Equal(url, context.Response.Headers["HX-Redirect"]);
    }

    [Fact]
    public void HxRefresh_SetsResponseHeaderToTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        context.Response.HxRefresh();

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("HX-Refresh"));
        Assert.Equal("true", context.Response.Headers["HX-Refresh"]);
    }

    [Fact]
    public void HxRetarget_SetsResponseHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var selector = "#notification-area";

        // Act
        context.Response.HxRetarget(selector);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("HX-Retarget"));
        Assert.Equal(selector, context.Response.Headers["HX-Retarget"]);
    }

    [Fact]
    public void HxReswap_SetsResponseHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var swapStrategy = "beforebegin";

        // Act
        context.Response.HxReswap(swapStrategy);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("HX-Reswap"));
        Assert.Equal(swapStrategy, context.Response.Headers["HX-Reswap"]);
    }

    #endregion
}
