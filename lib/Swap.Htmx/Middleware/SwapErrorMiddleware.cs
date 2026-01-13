using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Swap.Htmx.Models;

namespace Swap.Htmx.Middleware;

/// <summary>
/// Middleware that intercepts unhandled exceptions in HTMX requests and renders an error partial
/// (e.g. a toast or modal) instead of breaking the UI with a full HTML error page.
/// </summary>
public class SwapErrorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SwapHtmxOptions _options;
    private readonly ILogger<SwapErrorMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public SwapErrorMiddleware(
        RequestDelegate next, 
        SwapHtmxOptions options, 
        ILogger<SwapErrorMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _options = options;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Only handle if:
            // 1. Error handling is enabled
            // 2. It is an HTMX request
            // 3. Response hasn't started yet
            if (!_options.ErrorHandling.Enabled || 
                !context.Request.IsHtmxRequest() || 
                context.Response.HasStarted)
            {
                throw;
            }

            _logger.LogError(ex, "Unhandled exception in HTMX request");

            // Capture error details
            var showDetails = _options.ErrorHandling.ShowExceptionDetails || _env.IsDevelopment();
            var errorMessage = showDetails ? ex.Message : "An unexpected error occurred.";
            var errorModel = new SwapErrorModel 
            { 
                Message = errorMessage, 
                Exception = showDetails ? ex : null,
                RequestId = context.TraceIdentifier
            };

            // Render the error view
            context.Response.Clear();
            context.Response.StatusCode = 200; // Return 200 so HTMX processes the swap (OOB or Retarget)
            context.Response.ContentType = "text/html";
            
            // To prevent the error content from replacing the target (if using OOB),
            // we ideally want to just perform an OOB swap of the error container.
            // If the user hasn't set up OOB, we might break things.
            // Safe default: Use Reswap = "none" and rely on OOB.
            context.Response.Headers[HxHeaders.Reswap] = "none";

            // Render the partial
            await RenderErrorViewAsync(context, _options.ErrorHandling.ErrorViewName, errorModel);
        }
    }

    private async Task RenderErrorViewAsync(HttpContext context, string viewName, SwapErrorModel model)
    {
        var services = context.RequestServices;
        var viewEngine = services.GetRequiredService<ICompositeViewEngine>();
        var tempDataProvider = services.GetRequiredService<ITempDataProvider>();
        
        var actionContext = new ActionContext(context, context.GetRouteData() ?? new RouteData(), new ActionDescriptor());
        var viewResult = viewEngine.FindView(actionContext, viewName, false);

        if (!viewResult.Success)
        {
            // Fallback if view not found: Just return a simple error div OOB
            await context.Response.WriteAsync($@"
                <div id=""swap-error-toast"" hx-swap-oob=""true"" class=""swap-error-toast"" style=""position:fixed; top:20px; right:20px; background:#dc3545; color:white; padding:15px; border-radius:4px; box-shadow:0 2px 10px rgba(0,0,0,0.2); z-index:9999;"">
                    <strong>Error:</strong> {model.Message}
                    <button onclick=""this.parentElement.remove()"" style=""margin-left:10px; background:none; border:none; color:white; cursor:pointer;"">&times;</button>
                </div>");
            return;
        }

        var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = model
        };

        using (var writer = new System.IO.StringWriter())
        {
            var viewContext = new ViewContext(
                actionContext,
                viewResult.View,
                viewDictionary,
                new TempDataDictionary(context, tempDataProvider),
                writer,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);
            await context.Response.WriteAsync(writer.ToString());
        }
    }
}

/// <summary>
/// Model passed to the error view.
/// </summary>
public class SwapErrorModel
{
    public string Message { get; set; } = "";
    public Exception? Exception { get; set; }
    public string RequestId { get; set; } = "";
}
