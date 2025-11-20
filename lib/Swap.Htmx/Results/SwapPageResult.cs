using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using Swap.Htmx.Models;
using System.Text;
using System.Text.Json;

namespace Swap.Htmx.Results;

/// <summary>
/// Action result that executes a SwapResponseBuilder for Razor Pages.
/// </summary>
public sealed class SwapPageResult : ActionResult
{
    private readonly SwapResponseBuilder _builder;
    private readonly PageModel _pageModel;

    internal SwapPageResult(SwapResponseBuilder builder, PageModel pageModel)
    {
        _builder = builder;
        _pageModel = pageModel;
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        var logger = context.HttpContext.RequestServices.GetService<ILogger<SwapPageResult>>();
        
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
        var eventBus = context.HttpContext.RequestServices.GetService<ISwapEventBus>();
        foreach (var trigger in _builder.Triggers)
        {
            if (eventBus != null)
            {
                logger?.LogDebug("[SwapPageResult] Emitting event to bus: {EventName}", trigger.EventName);
                eventBus.Emit(new EventKey(trigger.EventName), trigger.Payload);
            }
            
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
                _pageModel.ViewData[oobKey] = html;
            }
        }

        // 5. Render main view (if one is specified) or just OOB swaps
        if (!string.IsNullOrEmpty(_builder.ViewName) || _builder.Model != null)
        {
            // For Razor Pages, we typically render a PartialView when doing HTMX swaps
            var viewResult = new PartialViewResult
            {
                ViewName = _builder.ViewName,
                ViewData = _pageModel.ViewData,
                TempData = _pageModel.TempData
            };

            if (_builder.Model != null)
            {
                viewResult.ViewData.Model = _builder.Model;
            }

            await viewResult.ExecuteResultAsync(context);
        }
        else if (_builder.OobSwaps.Count > 0)
        {
            var htmlContent = new StringBuilder();
            
            foreach (var key in _pageModel.ViewData.Keys.Where(k => k.StartsWith("Oob_")))
            {
                var oobHtml = _pageModel.ViewData[key] as string;
                if (!string.IsNullOrEmpty(oobHtml))
                {
                    htmlContent.Append(oobHtml);
                }
            }
            
            var contentResult = new ContentResult
            {
                Content = htmlContent.ToString(),
                ContentType = "text/html",
                StatusCode = 200
            };
            
            await contentResult.ExecuteResultAsync(context);
        }
        else
        {
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

        // Try to find the view
        // For Razor Pages, partials are often in the same folder or Shared
        var viewResult = viewEngine.FindView(context, oob.ViewName, isMainPage: false);
        
        if (!viewResult.Success)
        {
            var options = context.HttpContext.RequestServices.GetService<SwapHtmxOptions>();
            var searchPaths = options?.PartialViewSearchPaths ?? new List<string> { "Shared" };
            
            // Also check Pages/Shared
            var searchLocations = searchPaths
                .Select(path => $"~/Pages/{path}/{oob.ViewName}.cshtml")
                .Concat(searchPaths.Select(path => $"~/Views/{path}/{oob.ViewName}.cshtml"))
                .ToList();

            foreach (var location in searchLocations)
            {
                viewResult = viewEngine.GetView(executingFilePath: null, viewPath: location, isMainPage: false);
                if (viewResult.Success)
                    break;
            }

            if (!viewResult.Success)
            {
                var searchedPaths = string.Join(", ", searchLocations);
                throw new InvalidOperationException($"Could not find view '{oob.ViewName}' for OOB swap. Searched: {searchedPaths}");
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
