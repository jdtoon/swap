using Microsoft.AspNetCore.Http;
using NetMX.AspNetCore.Mvc.Htmx;

namespace NetMX.AspNetCore.Mvc.Tests.Htmx;

public class HtmxRequestExtensionsTests
{
    [Fact]
    public void IsHtmx_WithHxRequestHeader_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-Request"] = "true";

        // Act
        var result = context.Request.IsHtmx();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHtmx_WithoutHxRequestHeader_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var result = context.Request.IsHtmx();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsBoosted_WithBoostedHeader_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-Boosted"] = "true";

        // Act
        var result = context.Request.IsBoosted();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsBoosted_WithoutBoostedHeader_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var result = context.Request.IsBoosted();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHistoryRestoreRequest_WithHeaderTrue_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-History-Restore-Request"] = "true";

        // Act
        var result = context.Request.IsHistoryRestoreRequest();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetCurrentUrl_WithHeader_ReturnsUrl()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var expectedUrl = "https://example.com/page";
        context.Request.Headers["HX-Current-URL"] = expectedUrl;

        // Act
        var result = context.Request.GetCurrentUrl();

        // Assert
        Assert.Equal(expectedUrl, result);
    }

    [Fact]
    public void GetCurrentUrl_WithoutHeader_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var result = context.Request.GetCurrentUrl();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetPrompt_WithHeader_ReturnsPromptValue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var promptValue = "User entered this";
        context.Request.Headers["HX-Prompt"] = promptValue;

        // Act
        var result = context.Request.GetPrompt();

        // Assert
        Assert.Equal(promptValue, result);
    }

    [Fact]
    public void GetTarget_WithHeader_ReturnsTargetId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var targetId = "my-target-div";
        context.Request.Headers["HX-Target"] = targetId;

        // Act
        var result = context.Request.GetTarget();

        // Assert
        Assert.Equal(targetId, result);
    }

    [Fact]
    public void GetTriggerName_WithHeader_ReturnsTriggerName()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var triggerName = "submit-button";
        context.Request.Headers["HX-Trigger-Name"] = triggerName;

        // Act
        var result = context.Request.GetTriggerName();

        // Assert
        Assert.Equal(triggerName, result);
    }

    [Fact]
    public void GetTriggerId_WithHeader_ReturnsTriggerId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var triggerId = "btn-submit";
        context.Request.Headers["HX-Trigger"] = triggerId;

        // Act
        var result = context.Request.GetTriggerId();

        // Assert
        Assert.Equal(triggerId, result);
    }
}
