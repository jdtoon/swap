using Swap.Htmx.Middleware;
using Xunit;

namespace Swap.Htmx.Tests;

/// <summary>
/// Tests for the error-boundary fallback output. The fallback builds raw HTML (not a Razor view),
/// so it must HTML-encode any values it interpolates to avoid reflected XSS.
/// </summary>
public class SwapErrorMiddlewareTests
{
    [Fact]
    public void BuildFallbackErrorHtml_HtmlEncodesValues_PreventingReflectedXss()
    {
        var html = SwapErrorMiddleware.BuildFallbackErrorHtml(new SwapErrorModel
        {
            Message = "<script>alert('xss')</script>",
            RequestId = "trace\"><img src=x onerror=alert(1)>"
        });

        Assert.DoesNotContain("<script>alert", html);
        Assert.DoesNotContain("<img src=x", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void BuildFallbackErrorHtml_IncludesCorrelationId()
    {
        var html = SwapErrorMiddleware.BuildFallbackErrorHtml(new SwapErrorModel
        {
            Message = "An unexpected error occurred.",
            RequestId = "REQ-12345"
        });

        Assert.Contains("REQ-12345", html);
    }
}
