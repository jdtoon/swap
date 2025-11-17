using Microsoft.AspNetCore.Http;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapToastExtensionsTests
{
    [Fact]
    public void ShowToast_AppendsHxTriggerHeader_WhenNoExistingHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = context.Response;

        // Act
        response.ShowToast("Test message", ToastType.Info);

        // Assert
        Assert.True(response.Headers.ContainsKey("HX-Trigger"));
        var headerValue = response.Headers["HX-Trigger"].ToString();
        Assert.Contains("showToast", headerValue);
        Assert.Contains("Test message", headerValue);
        Assert.Contains("\"type\": \"info\"", headerValue);
    }

    [Fact]
    public void ShowToast_MergesWithExistingJsonObject()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = context.Response;
        response.Headers["HX-Trigger"] = "{\"someEvent\": null}";

        // Act
        response.ShowToast("Test message", ToastType.Success);

        // Assert
        var headerValue = response.Headers["HX-Trigger"].ToString();
        Assert.Contains("someEvent", headerValue);
        Assert.Contains("showToast", headerValue);
        Assert.Contains("Test message", headerValue);
        Assert.Contains("\"type\": \"success\"", headerValue);
    }

    [Fact]
    public void ShowToast_MergesWithExistingCommaSeparatedEvents()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = context.Response;
        response.Headers["HX-Trigger"] = "event1, event2";

        // Act
        response.ShowToast("Test message", ToastType.Warning);

        // Assert
        var headerValue = response.Headers["HX-Trigger"].ToString();
        Assert.Contains("event1", headerValue);
        Assert.Contains("event2", headerValue);
        Assert.Contains("showToast", headerValue);
        Assert.Contains("Test message", headerValue);
        Assert.Contains("\"type\": \"warning\"", headerValue);
    }

    [Fact]
    public void ShowToast_EscapesJsonSpecialCharacters()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = context.Response;
        var message = "Test \"quoted\" message\nwith newline\tand tab";

        // Act
        response.ShowToast(message, ToastType.Error);

        // Assert
        var headerValue = response.Headers["HX-Trigger"].ToString();
        Assert.Contains("\\\"quoted\\\"", headerValue);
        Assert.Contains("\\n", headerValue);
        Assert.Contains("\\t", headerValue);
    }

    [Theory]
    [InlineData(ToastType.Success, "success")]
    [InlineData(ToastType.Error, "error")]
    [InlineData(ToastType.Warning, "warning")]
    [InlineData(ToastType.Info, "info")]
    public void ShowToast_IncludesCorrectType(ToastType type, string expectedTypeString)
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = context.Response;

        // Act
        response.ShowToast("Test", type);

        // Assert
        var headerValue = response.Headers["HX-Trigger"].ToString();
        Assert.Contains($"\"type\": \"{expectedTypeString}\"", headerValue);
    }

    [Theory]
    [InlineData(ToastPosition.TopRight, "top-right")]
    [InlineData(ToastPosition.TopLeft, "top-left")]
    [InlineData(ToastPosition.BottomRight, "bottom-right")]
    [InlineData(ToastPosition.BottomLeft, "bottom-left")]
    public void ShowToast_IncludesCorrectPosition(ToastPosition position, string expectedPositionString)
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = context.Response;

        // Act
        response.ShowToast("Test", ToastType.Info, position);

        // Assert
        var headerValue = response.Headers["HX-Trigger"].ToString();
        Assert.Contains($"\"position\": \"{expectedPositionString}\"", headerValue);
    }

    [Fact]
    public void ShowSuccessToast_SetsSuccessType()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = context.Response;

        // Act
        response.ShowSuccessToast("Success message");

        // Assert
        var headerValue = response.Headers["HX-Trigger"].ToString();
        Assert.Contains("\"type\": \"success\"", headerValue);
        Assert.Contains("Success message", headerValue);
    }

    [Fact]
    public void ShowErrorToast_SetsErrorType()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = context.Response;

        // Act
        response.ShowErrorToast("Error message");

        // Assert
        var headerValue = response.Headers["HX-Trigger"].ToString();
        Assert.Contains("\"type\": \"error\"", headerValue);
        Assert.Contains("Error message", headerValue);
    }

    [Fact]
    public void ShowWarningToast_SetsWarningType()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = context.Response;

        // Act
        response.ShowWarningToast("Warning message");

        // Assert
        var headerValue = response.Headers["HX-Trigger"].ToString();
        Assert.Contains("\"type\": \"warning\"", headerValue);
        Assert.Contains("Warning message", headerValue);
    }

    [Fact]
    public void ShowInfoToast_SetsInfoType()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = context.Response;

        // Act
        response.ShowInfoToast("Info message");

        // Assert
        var headerValue = response.Headers["HX-Trigger"].ToString();
        Assert.Contains("\"type\": \"info\"", headerValue);
        Assert.Contains("Info message", headerValue);
    }

    [Fact]
    public void ShowToast_DefaultsToInfoTypeAndTopRightPosition()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = context.Response;

        // Act
        response.ShowToast("Default test");

        // Assert
        var headerValue = response.Headers["HX-Trigger"].ToString();
        Assert.Contains("\"type\": \"info\"", headerValue);
        Assert.Contains("\"position\": \"top-right\"", headerValue);
    }

    [Fact]
    public void ShowToast_HandlesMultipleToastsInSequence()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = context.Response;

        // Act - First toast
        response.ShowToast("First message", ToastType.Info);
        var firstHeader = response.Headers["HX-Trigger"].ToString();

        // Act - Second toast (overwrites)
        response.ShowToast("Second message", ToastType.Success);
        var secondHeader = response.Headers["HX-Trigger"].ToString();

        // Assert - Second call should overwrite/merge
        Assert.Contains("showToast", secondHeader);
        Assert.Contains("Second message", secondHeader);
    }

    [Fact]
    public void ShowToast_SkipsToast_WhenHistoryRestoreRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-History-Restore-Request"] = "true";
        var response = context.Response;

        // Act
        response.ShowToast("Test message", ToastType.Info);

        // Assert - No HX-Trigger header should be added
        Assert.False(response.Headers.ContainsKey("HX-Trigger"));
    }

    [Fact]
    public void ShowToast_AddsCacheControlHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = context.Response;

        // Act
        response.ShowToast("Test message", ToastType.Info);

        // Assert
        Assert.True(response.Headers.ContainsKey("Cache-Control"));
        Assert.Contains("no-cache", response.Headers["Cache-Control"].ToString());
        Assert.Contains("no-store", response.Headers["Cache-Control"].ToString());
        Assert.Contains("must-revalidate", response.Headers["Cache-Control"].ToString());
    }

    [Fact]
    public void ShowToast_NoCacheControlHeaders_WhenHistoryRestoreRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-History-Restore-Request"] = "true";
        var response = context.Response;

        // Act
        response.ShowToast("Test message", ToastType.Info);

        // Assert - No cache headers should be added on history restore
        Assert.False(response.Headers.ContainsKey("Cache-Control"));
    }
}

