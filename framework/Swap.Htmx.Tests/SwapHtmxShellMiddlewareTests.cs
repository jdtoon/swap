using Microsoft.AspNetCore.Http;
using System.Text;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapHtmxShellMiddlewareTests
{
    private static async Task<string> GetResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await new StreamReader(context.Response.Body).ReadToEndAsync();
    }

    [Fact]
    public async Task InvokeAsync_HtmxRequestWithFullPage_ReturnsError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-Request"] = "true";
        context.Request.Path = "/test";
        context.Request.Method = "GET";
        
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var fullPageHtml = @"<!DOCTYPE html>
<html>
<head><title>Test</title></head>
<body><div>Content</div></body>
</html>";

        var middleware = new SwapHtmxShellMiddleware(async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/html";
            await ctx.Response.WriteAsync(fullPageHtml);
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);
        
        responseBody.Seek(0, SeekOrigin.Begin);
        var response = await new StreamReader(responseBody).ReadToEndAsync();
        Assert.Contains("HTMX Shell Middleware Error", response);
        Assert.Contains("Full page returned for HTMX request", response);
        Assert.Contains(context.Request.Path, response);
    }

    [Fact]
    public async Task InvokeAsync_HtmxRequestWithPartialView_PassesThrough()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-Request"] = "true";
        
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var partialHtml = "<div>Partial content without full HTML structure</div>";

        var middleware = new SwapHtmxShellMiddleware(async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/html";
            await ctx.Response.WriteAsync(partialHtml);
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);
        
        responseBody.Seek(0, SeekOrigin.Begin);
        var response = await new StreamReader(responseBody).ReadToEndAsync();
        Assert.Equal(partialHtml, response);
    }

    [Fact]
    public async Task InvokeAsync_BoostedRequest_PassesThroughEvenWithFullPage()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-Request"] = "true";
        context.Request.Headers["HX-Boosted"] = "true";
        
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var fullPageHtml = @"<!DOCTYPE html>
<html>
<head><title>Test</title></head>
<body><div>Content</div></body>
</html>";

        var middleware = new SwapHtmxShellMiddleware(async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/html";
            await ctx.Response.WriteAsync(fullPageHtml);
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);
        var response = await GetResponseBody(context);
        Assert.Equal(fullPageHtml, response);
    }

    [Fact]
    public async Task InvokeAsync_NonHtmxRequest_PassesThroughFullPage()
    {
        // Arrange
        var context = new DefaultHttpContext();
        
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var fullPageHtml = @"<!DOCTYPE html>
<html>
<head><title>Test</title></head>
<body><div>Content</div></body>
</html>";

        var middleware = new SwapHtmxShellMiddleware(async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/html";
            await ctx.Response.WriteAsync(fullPageHtml);
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);
        var response = await GetResponseBody(context);
        Assert.Equal(fullPageHtml, response);
    }

    [Fact]
    public async Task InvokeAsync_HtmxRequestWithHtmlTagAndHeadTag_ReturnsError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-Request"] = "true";
        context.Request.Path = "/test";
        
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var fullPageHtml = @"<html lang='en'>
<head><meta charset='utf-8'></head>
<body><div>Content</div></body>
</html>";

        var middleware = new SwapHtmxShellMiddleware(async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/html";
            await ctx.Response.WriteAsync(fullPageHtml);
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);
        var response = await GetResponseBody(context);
        Assert.Contains("Full page returned for HTMX request", response);
    }

    [Fact]
    public async Task InvokeAsync_HtmxRequestWithOnlyHtmlTag_PassesThrough()
    {
        // Arrange - HTML tag without HEAD tag is considered partial
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-Request"] = "true";
        
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var partialHtml = "<html><body><div>Content without head</div></body></html>";

        var middleware = new SwapHtmxShellMiddleware(async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/html";
            await ctx.Response.WriteAsync(partialHtml);
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);
        var response = await GetResponseBody(context);
        Assert.Equal(partialHtml, response);
    }

    [Fact]
    public async Task InvokeAsync_HtmxRequestNon200Status_PassesThrough()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-Request"] = "true";
        
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var errorHtml = @"<!DOCTYPE html><html><head></head><body>Error</body></html>";

        var middleware = new SwapHtmxShellMiddleware(async (ctx) =>
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.ContentType = "text/html";
            await ctx.Response.WriteAsync(errorHtml);
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(404, context.Response.StatusCode);
        var response = await GetResponseBody(context);
        Assert.Equal(errorHtml, response);
    }

    [Fact]
    public async Task InvokeAsync_HtmxRequestNonHtmlContentType_PassesThrough()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-Request"] = "true";
        
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var jsonContent = "{\"message\": \"test\"}";

        var middleware = new SwapHtmxShellMiddleware(async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(jsonContent);
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        var response = await GetResponseBody(context);
        Assert.Equal(jsonContent, response);
    }

    [Fact]
    public async Task InvokeAsync_ErrorInPipeline_RethrowsException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-Request"] = "true";
        
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var middleware = new SwapHtmxShellMiddleware(async (ctx) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Test exception");
        });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await middleware.InvokeAsync(context);
        });
    }

    [Fact]
    public async Task InvokeAsync_IncludesRequestDetailsInError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["HX-Request"] = "true";
        context.Request.Headers["HX-Target"] = "content-area";
        context.Request.Headers["HX-Trigger"] = "submit-btn";
        context.Request.Path = "/articles/create";
        context.Request.Method = "POST";
        
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var fullPageHtml = "<!DOCTYPE html><html><head></head><body>Content</body></html>";

        var middleware = new SwapHtmxShellMiddleware(async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/html";
            await ctx.Response.WriteAsync(fullPageHtml);
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var response = await GetResponseBody(context);
        Assert.Contains("/articles/create", response);
        Assert.Contains("POST", response);
        Assert.Contains("content-area", response);
        Assert.Contains("submit-btn", response);
    }
}
