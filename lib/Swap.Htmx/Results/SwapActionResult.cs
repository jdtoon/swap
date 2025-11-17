using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Swap.Htmx.Extensions;
using Swap.Htmx.Models;
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
        
        // 1. Apply toasts
        foreach (var toast in _builder.Toasts)
        {
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

        // 2. Apply custom triggers
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

        // 3. Render OOB swaps and store in ViewData
        if (_builder.OobSwaps.Count > 0)
        {
            foreach (var oob in _builder.OobSwaps)
            {
                var html = await RenderOobSwapAsync(context, oob);
                var oobKey = $"Oob_{oob.TargetId}_{Guid.NewGuid():N}";
                _controller.ViewData[oobKey] = html;
            }
        }

        // 4. Render main view
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

    private async Task<string> RenderOobSwapAsync(ActionContext context, OobSwap oob)
    {
        var viewEngine = context.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
        var modelMetadataProvider = context.HttpContext.RequestServices.GetRequiredService<IModelMetadataProvider>();
        var viewData = new ViewDataDictionary(modelMetadataProvider, context.ModelState)
        {
            Model = oob.Model
        };

        var viewResult = viewEngine.FindView(context, oob.ViewName, isMainPage: false);
        if (!viewResult.Success)
        {
            throw new InvalidOperationException($"Could not find view '{oob.ViewName}' for OOB swap.");
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
