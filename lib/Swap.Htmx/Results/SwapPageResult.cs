using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swap.Htmx.Diagnostics;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using Swap.Htmx.Models;
using Swap.Htmx.State;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
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
        using var activity = SwapTelemetry.ActivitySource.StartActivity("Swap.Htmx.PageResultExecute");
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
            logger?.Toast(toast.Type.ToString(), toast.Message);
            
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
                logger?.Trigger(trigger.EventName, trigger.Payload?.GetType().Name ?? "null");
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

        // 4. Render OOB swaps
        var oobContent = new List<string>();
        if (_builder.OobSwaps.Count > 0)
        {
            foreach (var oob in _builder.OobSwaps)
            {
                var html = await RenderOobSwapAsync(context, oob);
                oobContent.Add(html);
            }
        }

        // 4b. Render SwapState as OOB if configured
        if (_builder.State != null)
        {
            var stateHtml = RenderStateAsOob(_builder.State);
            oobContent.Add(stateHtml);
        }

        // 5. Render main view (if one is specified) or just OOB swaps
        if (!string.IsNullOrEmpty(_builder.ViewName) || _builder.Model != null)
        {
            // For Razor Pages, render the partial view manually so we can append OOB content
            var viewEngine = context.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
            var viewName = _builder.ViewName ?? context.RouteData.Values["page"]?.ToString();

            if (string.IsNullOrEmpty(viewName))
            {
                throw new InvalidOperationException("View name could not be determined. Please specify a view name explicitly.");
            }

            var viewResult = viewEngine.FindView(context, viewName, isMainPage: false);
            if (!viewResult.Success)
            {
                viewResult = viewEngine.GetView(null, viewName, isMainPage: false);
            }

            if (!viewResult.Success)
            {
                throw new InvalidOperationException($"Could not find view '{viewName}'.");
            }

            if (_builder.Model != null)
            {
                _pageModel.ViewData.Model = _builder.Model;
            }

            await using var writer = new StringWriter();
            var viewContext = new ViewContext(
                context,
                viewResult.View,
                _pageModel.ViewData,
                _pageModel.TempData,
                writer,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);
            
            // Build final response: main view + OOB swaps
            var responseBuilder = new StringBuilder();
            responseBuilder.Append(writer.ToString());
            
            foreach (var oob in oobContent)
            {
                responseBuilder.Append(oob);
            }
            
            response.ContentType = "text/html; charset=utf-8";
            await response.WriteAsync(responseBuilder.ToString());
        }
        else if (oobContent.Count > 0)
        {
            var htmlContent = new StringBuilder();
            
            foreach (var oob in oobContent)
            {
                htmlContent.Append(oob);
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
        var html = sw.ToString().Trim();

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

        // If the rendered HTML already contains hx-swap-oob, return as-is
        if (html.Contains("hx-swap-oob"))
        {
            return html;
        }
        
        // If the rendered HTML already has an element with the target ID, add the oob attribute to it
        var idPattern = $"id=\"{oob.TargetId}\"";
        if (html.Contains(idPattern))
        {
            // Insert hx-swap-oob attribute after the id attribute
            return html.Replace(idPattern, $"{idPattern} hx-swap-oob=\"{swapModeStr}\"");
        }

        // Otherwise wrap in a div (fallback for views without the id)
        return $"<div id=\"{oob.TargetId}\" hx-swap-oob=\"{swapModeStr}\">{html}</div>";
    }

    /// <summary>
    /// Renders a SwapState as an OOB swap element (no view file required).
    /// </summary>
    private static string RenderStateAsOob(SwapState state)
    {
        var encoder = HtmlEncoder.Default;
        var sb = new StringBuilder();
        
        sb.Append($"<div id=\"{encoder.Encode(state.ContainerId)}\" data-swap-state hx-swap-oob=\"true\" style=\"display: none;\">");
        
        foreach (var kvp in state.GetStateValues())
        {
            var fieldValue = FormatStateValue(kvp.Value);
            sb.Append($"<input type=\"hidden\" name=\"{encoder.Encode(kvp.Key)}\" value=\"{encoder.Encode(fieldValue)}\" />");
        }
        
        sb.Append("</div>");
        
        return sb.ToString();
    }

    private static string FormatStateValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            bool b => b ? "true" : "false",
            decimal d => d.ToString(CultureInfo.InvariantCulture),
            double dbl => dbl.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString(CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("O"),
            DateTimeOffset dto => dto.ToString("O"),
            Enum e => e.ToString(),
            _ => value.ToString() ?? string.Empty
        };
    }
}
