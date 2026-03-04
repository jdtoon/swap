using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

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
        using var activity = SwapTelemetry.ActivitySource.StartActivity("Swap.Htmx.ActionResultExecute");
        var response = context.HttpContext.Response;
        var logger = context.HttpContext.RequestServices.GetService<ILogger<SwapActionResult>>();

        // Development diagnostics (warn-once) for common first-run issues.
        context.HttpContext.RequestServices.GetService<ISwapDiagnostics>()?.ValidateResponse(_builder);
        
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

        // 3. Apply custom triggers - emit to event bus FIRST for SSE processing
        var eventBus = context.HttpContext.RequestServices.GetService<ISwapEventBus>();
        foreach (var trigger in _builder.Triggers)
        {
            // Emit to event bus so SSE middleware can pick it up
            if (eventBus != null)
            {
                logger?.Trigger(trigger.EventName, trigger.Payload?.GetType().Name ?? "null");
                eventBus.Emit(new EventKey(trigger.EventName), trigger.Payload);
            }
            
            // Also add to HX-Trigger header for client-side handling
            if (trigger.Payload == null)
            {
                response.HxTrigger(trigger.EventName);
            }
            else
            {
                response.HxTrigger(trigger.EventName, trigger.Payload);
            }
        }

        // 4. Render OOB swaps (parallelized for performance)
        var oobContent = new List<string>();
        if (_builder.OobSwaps.Count > 0)
        {
            var oobTasks = _builder.OobSwaps.Select(oob => RenderOobSwapAsync(context, oob)).ToArray();
            var results = await Task.WhenAll(oobTasks);
            oobContent.AddRange(results);
        }

        // 4b. Render SwapState as OOB if configured
        if (_builder.State != null)
        {
            var protectionProvider = context.HttpContext.RequestServices.GetService<IDataProtectionProvider>();
            var stateHtml = SwapStateRenderer.RenderAsOob(_builder.State, protectionProvider);
            oobContent.Add(stateHtml);
        }

        // 5. Render main view (if one is specified) or just OOB swaps
        if (!string.IsNullOrEmpty(_builder.ViewName) || _builder.Model != null)
        {
            // Has a main view to render - render manually so we can append OOB content
            var viewEngine = context.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
            var viewName = _builder.ViewName ?? context.RouteData.Values["action"]?.ToString();

            if (string.IsNullOrEmpty(viewName))
            {
                throw new InvalidOperationException(
                    "View name could not be determined from route data. " +
                    "Fix: call WithView(\"YourViewName\", model) (or WithView(\"~/Views/Shared/YourViewName.cshtml\", model)).");
            }

            var searchedLocations = new List<string>();
            var viewResult = viewEngine.FindView(context, viewName, isMainPage: false);
            searchedLocations.AddRange(viewResult.SearchedLocations);
            if (!viewResult.Success)
            {
                viewResult = viewEngine.GetView(null, viewName, isMainPage: false);
                searchedLocations.AddRange(viewResult.SearchedLocations);
            }

            if (!viewResult.Success)
            {
                var searched = string.Join(", ", searchedLocations.Distinct());
                logger?.LogError(
                    "[Swap.Htmx] Could not find view {ViewName}. Searched: {SearchedLocations}. " +
                    "Fix: ensure the view exists under Views/Shared or provide an app-relative path (~/Views/.../View.cshtml) via WithView().",
                    viewName,
                    searched);
                throw new InvalidOperationException(
                    $"Could not find view '{viewName}'. Searched: {searched}. " +
                    "Fix: ensure the view exists under Views/Shared or provide an app-relative path (~/Views/.../View.cshtml) via WithView().");
            }

            if (_builder.Model != null)
            {
                _controller.ViewData.Model = _builder.Model;
            }

            await using var writer = new StringWriter();
            var viewContext = new ViewContext(
                context,
                viewResult.View,
                _controller.ViewData,
                _controller.TempData,
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
            // No main view, but we have OOB swaps
            var htmlContent = new StringBuilder();
            
            foreach (var oob in oobContent)
            {
                htmlContent.Append(oob);
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
        // Handle Delete mode specially - no view rendering needed
        if (oob.SwapMode == SwapMode.Delete)
        {
            return $"<div id=\"{oob.TargetId}\" hx-swap-oob=\"delete\"></div>";
        }

        var viewEngine = context.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
        var modelMetadataProvider = context.HttpContext.RequestServices.GetRequiredService<IModelMetadataProvider>();
        var viewData = new ViewDataDictionary(modelMetadataProvider, context.ModelState)
        {
            Model = oob.Model
        };

        // Try to find the view - first in current controller context
        var viewResult = viewEngine.FindView(context, oob.ViewName, isMainPage: false);
        
        if (!viewResult.Success)
        {
            // Get configured search paths from SwapHtmxOptions
            var options = context.HttpContext.RequestServices.GetService<SwapHtmxOptions>();
            var searchPaths = options?.PartialViewSearchPaths ?? new List<string> { "Shared" };
            
            // Build search locations from configured paths
            var searchLocations = searchPaths
                .Select(path => $"~/Views/{path}/{oob.ViewName}.cshtml")
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
                var logger = context.HttpContext.RequestServices.GetService<ILogger<SwapActionResult>>();
                logger?.LogError(
                    "[Swap.Htmx] Could not find view {ViewName} for OOB swap targeting #{TargetId}. Searched: {SearchedLocations}. " +
                    "Fix: move the partial under Views/Shared, provide an app-relative path, or configure SwapHtmxOptions.PartialViewSearchPaths.",
                    oob.ViewName,
                    oob.TargetId,
                    searchedPaths);

                throw new InvalidOperationException(
                    $"Could not find view '{oob.ViewName}' for OOB swap (target '#{oob.TargetId}'). Searched: {searchedPaths}. " +
                    "Fix: move the partial under Views/Shared, provide an app-relative path, or configure SwapHtmxOptions.PartialViewSearchPaths.");
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
        var swapOptions = context.HttpContext.RequestServices.GetService<SwapHtmxOptions>();
        if (swapOptions?.Diagnostics.WarnOnMissingOobTargets == true)
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<SwapActionResult>>();
            logger?.LogWarning(
                "[Swap.Htmx] OOB swap for target #{TargetId} rendered view {ViewName} without a matching id attribute. " +
            "Swap will be wrapped in a <div> with that id and hx-swap-oob=... . " +
                "If you intended to swap an existing element, ensure the rendered partial contains the target id or includes hx-swap-oob.",
                oob.TargetId,
                oob.ViewName);
        }
        return $"<div id=\"{oob.TargetId}\" hx-swap-oob=\"{swapModeStr}\">{html}</div>";
    }

}
