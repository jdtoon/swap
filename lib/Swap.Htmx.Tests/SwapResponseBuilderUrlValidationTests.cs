using Swap.Htmx.Models;
using Xunit;

namespace Swap.Htmx.Tests;

/// <summary>
/// Tests for URL validation in SwapResponseBuilder (WithRedirect, WithNavigation).
/// </summary>
public class SwapResponseBuilderUrlValidationTests
{
    [Theory]
    [InlineData("/dashboard")]
    [InlineData("/items/123")]
    [InlineData("/search?q=test")]
    [InlineData("items/edit")]
    [InlineData("/")]
    public void WithRedirect_AcceptsRelativeUrls(string url)
    {
        var builder = new SwapResponseBuilder().WithRedirect(url);
        Assert.Equal(url, builder.RedirectUrl);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JavaScript:void(0)")]
    [InlineData("JAVASCRIPT:alert('xss')")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("DATA:text/html;base64,abc")]
    [InlineData("vbscript:MsgBox(1)")]
    [InlineData("VBSCRIPT:MsgBox(1)")]
    public void WithRedirect_RejectsDangerousSchemes(string url)
    {
        var builder = new SwapResponseBuilder();
        Assert.Throws<ArgumentException>(() => builder.WithRedirect(url));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void WithRedirect_RejectsEmptyUrl(string url)
    {
        var builder = new SwapResponseBuilder();
        Assert.Throws<ArgumentException>(() => builder.WithRedirect(url));
    }

    [Theory]
    [InlineData("/dashboard")]
    [InlineData("/items/123")]
    [InlineData("/search?q=test")]
    public void WithNavigation_AcceptsRelativeUrls(string path)
    {
        var builder = new SwapResponseBuilder().WithNavigation(path);
        Assert.Equal(path, builder.Navigation!.Path);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("vbscript:MsgBox(1)")]
    public void WithNavigation_RejectsDangerousSchemes(string path)
    {
        var builder = new SwapResponseBuilder();
        Assert.Throws<ArgumentException>(() => builder.WithNavigation(path));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void WithNavigation_RejectsEmptyUrl(string path)
    {
        var builder = new SwapResponseBuilder();
        Assert.Throws<ArgumentException>(() => builder.WithNavigation(path));
    }

    [Fact]
    public void WithNavigation_PreservesAllOptions()
    {
        var builder = new SwapResponseBuilder()
            .WithNavigation("/test", target: "#sidebar", pushUrl: false);

        Assert.Equal("/test", builder.Navigation!.Path);
        Assert.Equal("#sidebar", builder.Navigation.Target);
        Assert.False(builder.Navigation.PushUrl);
    }

    [Fact]
    public void WithRedirect_CombinesWithOtherBuilderMethods()
    {
        var builder = new SwapResponseBuilder()
            .WithView("_Main")
            .WithRedirect("/dashboard")
            .WithSuccessToast("Done!");

        Assert.Equal("/dashboard", builder.RedirectUrl);
        Assert.Single(builder.Toasts);
    }

    [Theory]
    [InlineData("//evil.com")]
    [InlineData("//evil.com/path")]
    [InlineData("/\\evil.com")]
    [InlineData("\\\\evil.com")]
    [InlineData("\\/evil.com")]
    public void WithRedirect_RejectsProtocolRelativeUrls(string url)
    {
        // Protocol-relative URLs resolve to an off-site absolute URL in the browser (open redirect).
        var builder = new SwapResponseBuilder();
        Assert.Throws<ArgumentException>(() => builder.WithRedirect(url));
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://evil.com/x")]
    [InlineData("mailto:a@b.com")]
    public void WithRedirect_RejectsNonHttpSchemes(string url)
    {
        var builder = new SwapResponseBuilder();
        Assert.Throws<ArgumentException>(() => builder.WithRedirect(url));
    }

    [Theory]
    [InlineData("https://example.com/ok")]
    [InlineData("http://example.com")]
    public void WithRedirect_AcceptsAbsoluteHttpUrls(string url)
    {
        var builder = new SwapResponseBuilder().WithRedirect(url);
        Assert.Equal(url, builder.RedirectUrl);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("//evil.com")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    public void WithNavigation_HxLocationOptions_ValidatesPath(string path)
    {
        // The HxLocationOptions overload previously bypassed all URL validation.
        var builder = new SwapResponseBuilder();
        Assert.Throws<ArgumentException>(() =>
            builder.WithNavigation(new HxLocationOptions { Path = path }));
    }

    [Fact]
    public void WithNavigation_HxLocationOptions_AcceptsValidPath()
    {
        var builder = new SwapResponseBuilder()
            .WithNavigation(new HxLocationOptions { Path = "/dashboard" });

        Assert.Equal("/dashboard", builder.NavigationOptions!.Path);
    }
}
