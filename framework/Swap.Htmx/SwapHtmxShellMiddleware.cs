using Microsoft.AspNetCore.Http;
using System.Text;

namespace Swap.Htmx;

/// <summary>
/// Middleware that enforces HTMX shell behavior by catching full page responses
/// when partial views are expected. This helps debug issues where full pages
/// are accidentally returned for HTMX requests.
/// </summary>
public class SwapHtmxShellMiddleware
{
    private readonly RequestDelegate _next;

    public SwapHtmxShellMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if this is an HTMX request (not a boosted request)
        var isHtmxRequest = context.Request.Headers.ContainsKey("HX-Request");
        var isBoosted = context.Request.Headers.ContainsKey("HX-Boosted");

        if (isHtmxRequest && !isBoosted)
        {
            // Capture the response body
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);

                // Only validate HTML responses
                if (context.Response.StatusCode == 200 &&
                    context.Response.ContentType?.Contains("text/html") == true)
                {
                    responseBody.Seek(0, SeekOrigin.Begin);
                    var responseContent = await new StreamReader(responseBody).ReadToEndAsync();

                    // Check if response contains full HTML document markers
                    var hasDocType = responseContent.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase);
                    var hasHtmlTag = responseContent.Contains("<html", StringComparison.OrdinalIgnoreCase);
                    var hasHeadTag = responseContent.Contains("<head", StringComparison.OrdinalIgnoreCase);

                    if (hasDocType || (hasHtmlTag && hasHeadTag))
                    {
                        // Full page returned for HTMX request - this is likely a mistake
                        responseBody.SetLength(0); // Clear the captured response
                        responseBody.Seek(0, SeekOrigin.Begin);
                        
                        var errorHtml = @"
<div style='padding: 20px; background: #fee; border: 2px solid #c00; border-radius: 8px; margin: 20px; font-family: monospace;'>
    <h2 style='color: #c00; margin-top: 0;'>⚠️ HTMX Shell Middleware Error</h2>
    <p><strong>Full page returned for HTMX request</strong></p>
    <p>An HTMX request received a full HTML page instead of a partial view. This usually means:</p>
    <ul>
        <li>Controller is returning <code>View()</code> instead of <code>SwapView()</code> or <code>PartialView()</code></li>
        <li>Layout is being rendered when it shouldn't be</li>
        <li>Error page middleware is returning full page on exceptions</li>
    </ul>
    <p><strong>Request Details:</strong></p>
    <ul>
        <li>Path: <code>" + context.Request.Path + @"</code></li>
        <li>Method: <code>" + context.Request.Method + @"</code></li>
        <li>HX-Target: <code>" + context.Request.Headers["HX-Target"].FirstOrDefault() + @"</code></li>
        <li>HX-Trigger: <code>" + context.Request.Headers["HX-Trigger"].FirstOrDefault() + @"</code></li>
    </ul>
    <p><strong>Fix:</strong> Ensure controller inherits from <code>SwapController</code> and uses <code>SwapView()</code> instead of <code>View()</code>.</p>
</div>";

                        await context.Response.WriteAsync(errorHtml);
                        
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "text/html";
                        
                        // Copy error response to original stream
                        responseBody.Seek(0, SeekOrigin.Begin);
                        await responseBody.CopyToAsync(originalBodyStream);
                        return;
                    }

                    // Response is valid partial content - copy it back
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                }
                else
                {
                    // Non-200 or non-HTML response - pass through
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
            catch
            {
                // Error occurred - copy response as-is
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
        else
        {
            // Not an HTMX request or is boosted - pass through
            await _next(context);
        }
    }
}
