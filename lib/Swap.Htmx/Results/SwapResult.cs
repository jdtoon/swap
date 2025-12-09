using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swap.Htmx.Diagnostics;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using Swap.Htmx.Models;
using Swap.Htmx.State;
using System.Text;

namespace Swap.Htmx.Results;

/// <summary>
/// IResult implementation for Minimal APIs that executes a SwapResponseBuilder.
/// </summary>
public sealed class SwapResult : IResult
{
    private readonly SwapResponseBuilder _builder;

    public SwapResult(SwapResponseBuilder builder)
    {
        _builder = builder;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        using var activity = SwapTelemetry.ActivitySource.StartActivity("Swap.Htmx.ResultExecute");
        var logger = httpContext.RequestServices.GetService<ILogger<SwapResult>>();
        var response = httpContext.Response;

        // 1. Apply redirect if configured
        if (!string.IsNullOrEmpty(_builder.RedirectUrl))
        {
            response.HxRedirect(_builder.RedirectUrl);
        }

        // 1b. Apply SPA navigation if configured (HX-Location)
        if (_builder.Navigation != null)
        {
            var nav = _builder.Navigation;
            response.HxLocation(new HxLocationOptions
            {
                Path = nav.Path,
                Target = nav.Target,
                // Note: 'push' is not a standard HX-Location property, 
                // but we set HX-Push-Url separately if needed
            });
            if (!nav.PushUrl)
            {
                response.HxPreventPushUrl();
            }
        }
        else if (_builder.NavigationOptions != null)
        {
            response.HxLocation(_builder.NavigationOptions);
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
        var eventBus = httpContext.RequestServices.GetService<ISwapEventBus>();
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

        // 3.5. Apply client actions as triggers
        foreach (var action in _builder.ClientActions)
        {
            var payload = new { action = action.Action, target = action.Target, value = action.Value };
            response.HxTrigger("swap:clientAction", payload);
        }

        // Prepare ActionContext for view rendering
        var routeData = httpContext.GetRouteData() ?? new RouteData();
        var actionDescriptor = new ActionDescriptor();
        var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);

        // Prepare ViewData and TempData
        var modelMetadataProvider = httpContext.RequestServices.GetRequiredService<IModelMetadataProvider>();
        var viewData = new ViewDataDictionary(modelMetadataProvider, new ModelStateDictionary());
        if (_builder.Model != null)
        {
            viewData.Model = _builder.Model;
        }

        var tempDataProvider = httpContext.RequestServices.GetRequiredService<ITempDataProvider>();
        var tempData = new TempDataDictionary(httpContext, tempDataProvider);

        // 4. Render OOB swaps
        var oobContent = new List<string>();
        if (_builder.OobSwaps.Count > 0)
        {
            foreach (var oob in _builder.OobSwaps)
            {
                var html = await RenderOobSwapAsync(actionContext, viewData, tempData, oob);
                oobContent.Add(html);
            }
        }

        // 4b. Render SwapState as OOB if configured
        if (_builder.State != null)
        {
            var stateHtml = SwapStateRenderer.RenderAsOob(_builder.State);
            oobContent.Add(stateHtml);
        }

        // 5. Render main view or OOB content
        if (!string.IsNullOrEmpty(_builder.ViewName) || _builder.Model != null)
        {
            // Main view rendering
            var viewEngine = httpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
            var viewName = _builder.ViewName ?? actionContext.RouteData.Values["action"]?.ToString();

            if (string.IsNullOrEmpty(viewName))
            {
                throw new InvalidOperationException("View name could not be determined. Please specify a view name explicitly.");
            }

            var viewResult = viewEngine.FindView(actionContext, viewName, isMainPage: false);
            if (!viewResult.Success)
            {
                viewResult = viewEngine.GetView(null, viewName, isMainPage: false);
            }

            if (!viewResult.Success)
            {
                throw new InvalidOperationException($"Could not find view '{viewName}'.");
            }

            using var writer = new StringWriter();
            var viewContext = new ViewContext(
                actionContext,
                viewResult.View,
                viewData,
                tempData,
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
            // Only OOB swaps
            response.ContentType = "text/html; charset=utf-8";
            foreach (var html in oobContent)
            {
                await response.WriteAsync(html);
            }
        }
    }

    private async Task<string> RenderOobSwapAsync(
        ActionContext context, 
        ViewDataDictionary viewData, 
        ITempDataDictionary tempData, 
        OobSwap oob)
    {
        if (oob.SwapMode == SwapMode.Delete)
        {
            return $"<div id=\"{oob.TargetId}\" hx-swap-oob=\"delete\"></div>";
        }

        var viewEngine = context.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
        
        // Create a new ViewData for the OOB swap to avoid polluting the main one
        var oobViewData = new ViewDataDictionary(viewData)
        {
            Model = oob.Model
        };

        var viewResult = viewEngine.FindView(context, oob.ViewName, isMainPage: false);
        
        if (!viewResult.Success)
        {
            var options = context.HttpContext.RequestServices.GetService<SwapHtmxOptions>();
            var searchPaths = options?.PartialViewSearchPaths ?? new List<string> { "Shared" };
            
            var searchLocations = searchPaths
                .Select(path => $"~/Views/{path}/{oob.ViewName}.cshtml")
                .ToList();

            foreach (var location in searchLocations)
            {
                viewResult = viewEngine.GetView(executingFilePath: null, viewPath: location, isMainPage: false);
                if (viewResult.Success) break;
            }

            if (!viewResult.Success)
            {
                throw new InvalidOperationException($"Could not find view '{oob.ViewName}' for OOB swap.");
            }
        }

        using var sw = new StringWriter();
        var viewContext = new ViewContext(
            context,
            viewResult.View,
            oobViewData,
            tempData,
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

}
