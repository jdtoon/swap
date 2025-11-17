using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swap.Htmx.Extensions;
using Swap.Htmx.Models;
using System.Text;
using System.Text.Json;

namespace Swap.Htmx.Results;

/// <summary>
/// Action result that executes a SwapResponseBuilder, coordinating view rendering,
/// out-of-band swaps, toasts, and HX-Trigger events.
/// </summary>
public sealed class SwapActionResult : ActionResult
{
    private readonly SwapResponseBuilder _builder;
    private readonly Controller _controller;

    internal SwapActionResult(SwapResponseBuilder builder, Controller controller)
    {
        _builder = builder;
        _controller = controller;
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        var logger = context.HttpContext.RequestServices.GetService<ILogger<SwapActionResult>>();
        
        // 1. Apply redirect if configured
        if (!string.IsNullOrEmpty(_builder.RedirectUrl))
        {
            response.HxRedirect(_builder.RedirectUrl);
        }
        
        // 2. Apply toasts
        foreach (var toast in _builder.Toasts)
        {
            Dev.SwapDevLogger.LogToast(logger, toast.Type.ToString(), toast.Message);
            
            switch (toast.Type)
            {
                case ToastType.Success:
                    response.ShowSuccessToast(toast.Message);
                    break;
                case ToastType.Error:
                    response.ShowErrorToast(toast.Message);
                    break;
                case ToastType.Warning:
                    response.ShowWarningToast(toast.Message);
                    break;
                case ToastType.Info:
                    response.ShowInfoToast(toast.Message);
                    break;
            }
        }

        // 3. Apply custom triggers
        foreach (var trigger in _builder.Triggers)
        {
            if (trigger.Payload == null)
            {
                response.HxTrigger(trigger.EventName);
            }
            else
            {
                response.HxTrigger(trigger.EventName, trigger.Payload);
            }
        }

        // 4. Render OOB swaps and store in ViewData
        if (_builder.OobSwaps.Count > 0)
        {
            foreach (var oob in _builder.OobSwaps)
            {
                var html = await RenderOobSwapAsync(context, oob);
                var oobKey = $"Oob_{oob.TargetId}_{Guid.NewGuid():N}";
                _controller.ViewData[oobKey] = html;
            }
        }

        // 5. Render main view (if one is specified) or just OOB swaps
        if (!string.IsNullOrEmpty(_builder.ViewName) || _builder.Model != null)
        {
            // Has a main view to render
            var viewResult = new ViewResult
            {
                ViewName = _builder.ViewName,
                ViewData = _controller.ViewData,
                TempData = _controller.TempData
            };

            if (_builder.Model != null)
            {
                viewResult.ViewData.Model = _builder.Model;
            }

            await viewResult.ExecuteResultAsync(context);
        }
        else if (_builder.OobSwaps.Count > 0)
        {
            // No main view, but we have OOB swaps - use ContentResult to ensure proper header handling
            var htmlContent = new StringBuilder();
            
            foreach (var key in _controller.ViewData.Keys.Where(k => k.StartsWith("Oob_")))
            {
                var oobHtml = _controller.ViewData[key] as string;
                if (!string.IsNullOrEmpty(oobHtml))
                {
                    htmlContent.Append(oobHtml);
                }
            }
            
            if (context.HttpContext.Response.Headers.ContainsKey("HX-Trigger"))
            {
                var triggerValue = context.HttpContext.Response.Headers["HX-Trigger"].ToString();
                Dev.SwapDevLogger.LogHeader(logger, "HX-Trigger (before render)", triggerValue);
            }
            
            var contentResult = new ContentResult
            {
                Content = htmlContent.ToString(),
                ContentType = "text/html",
                StatusCode = 200
            };
            
            await contentResult.ExecuteResultAsync(context);
            
            if (context.HttpContext.Response.Headers.ContainsKey("HX-Trigger"))
            {
                var triggerValue = context.HttpContext.Response.Headers["HX-Trigger"].ToString();
                Dev.SwapDevLogger.LogHeader(logger, "HX-Trigger (after render)", triggerValue);
            }
        }
        else
        {
            // No main view and no OOB swaps - return empty content
            var emptyResult = new ContentResult 
            { 
                Content = "",
                ContentType = "text/html"
            };
            await emptyResult.ExecuteResultAsync(context);
        }
    }

    private async Task<string> RenderOobSwapAsync(ActionContext context, OobSwap oob)
    {
        var viewEngine = context.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
        var modelMetadataProvider = context.HttpContext.RequestServices.GetRequiredService<IModelMetadataProvider>();
        var viewData = new ViewDataDictionary(modelMetadataProvider, context.ModelState)
        {
            Model = oob.Model
        };

        // Try to find the view - first in current controller context, then in common locations
        var viewResult = viewEngine.FindView(context, oob.ViewName, isMainPage: false);
        
        if (!viewResult.Success)
        {
            // Try common partial locations if not found
            var searchLocations = new[]
            {
                $"~/Views/Shared/{oob.ViewName}.cshtml",
                $"~/Views/Cart/{oob.ViewName}.cshtml",
                $"~/Views/Products/{oob.ViewName}.cshtml",
                $"~/Views/Orders/{oob.ViewName}.cshtml"
            };

            foreach (var location in searchLocations)
            {
                viewResult = viewEngine.GetView(executingFilePath: null, viewPath: location, isMainPage: false);
                if (viewResult.Success)
                    break;
            }

            if (!viewResult.Success)
            {
                throw new InvalidOperationException($"Could not find view '{oob.ViewName}' for OOB swap.");
            }
        }

        await using var sw = new StringWriter();
        var tempDataProvider = context.HttpContext.RequestServices.GetRequiredService<ITempDataProvider>();
        var viewContext = new ViewContext(
            context,
            viewResult.View,
            viewData,
            new TempDataDictionary(context.HttpContext, tempDataProvider),
            sw,
            new HtmlHelperOptions()
        );

        await viewResult.View.RenderAsync(viewContext);
        var html = sw.ToString();

        // Wrap with hx-swap-oob attribute
        var swapModeStr = oob.SwapMode switch
        {
            SwapMode.OuterHTML => "true",
            SwapMode.InnerHTML => "innerHTML",
            SwapMode.BeforeBegin => "beforebegin",
            SwapMode.AfterBegin => "afterbegin",
            SwapMode.BeforeEnd => "beforeend",
            SwapMode.AfterEnd => "afterend",
            SwapMode.Delete => "delete",
            SwapMode.None => "none",
            _ => "true"
        };

        if (!html.Contains("hx-swap-oob"))
        {
            return $"<div id=\"{oob.TargetId}\" hx-swap-oob=\"{swapModeStr}\">{html}</div>";
        }

        return html;
    }
}
