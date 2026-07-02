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
        using var activity = SwapTelemetry.ActivitySource.StartActivity("Swap.Htmx.PageResultExecute");
        var response = context.HttpContext.Response;
        var logger = context.HttpContext.RequestServices.GetService<ILogger<SwapPageResult>>();

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

        // 2b. Stash flash toasts into TempData so they are re-emitted on the next response (survive redirects).
        if (_builder.FlashToasts.Count > 0)
        {
            Swap.Htmx.Middleware.SwapFlashHelper.Store(_pageModel.TempData, _builder.FlashToasts);
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

        // 4. Render OOB swaps sequentially. Each partial renders on the single request scope
        // (scoped DbContext, ViewData, the shared view-buffer pool); fanning them out with
        // Task.WhenAll races those scoped services and intermittently throws
        // "A second operation was started on this context instance". View rendering is CPU-bound
        // string building, so sequential rendering costs effectively nothing while removing the race.
        var coalescedOobSwaps = Swap.Htmx.Models.OobCoalescer.Coalesce(_builder.OobSwaps);
        var oobContent = new List<string>();
        foreach (var oob in coalescedOobSwaps)
        {
            oobContent.Add(await RenderOobSwapAsync(context, oob));
        }

        // 4a. Dependency-graph fragments for any invalidated topics (deduped; explicit OOB targets win).
        var fragmentRegistry = context.HttpContext.RequestServices.GetService<Swap.Htmx.Fragments.SwapFragmentRegistry>();
        foreach (var oob in Swap.Htmx.Fragments.FragmentResolver.Resolve(
                     fragmentRegistry, _builder.InvalidatedTopics, coalescedOobSwaps.Select(o => o.TargetId), context.HttpContext))
        {
            oobContent.Add(await RenderOobSwapAsync(context, oob));
        }

        // 4b. Render SwapState as OOB if configured. Always go through the request-scoped helper so
        // protected state is encrypted. Previously this passed no provider, rendering [SwapProtected]
        // state as plaintext on Razor Pages — the helper makes that impossible.
        if (_builder.State != null)
        {
            oobContent.Add(SwapStateRenderer.RenderAsOobForRequest(_builder.State, context.HttpContext));
        }

        // 5. Render main view (if one is specified) or just OOB swaps
        if (!string.IsNullOrEmpty(_builder.ViewName) || _builder.Model != null)
        {
            // For Razor Pages, render the partial view manually so we can append OOB content
            var viewEngine = context.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
            var viewName = _builder.ViewName ?? context.RouteData.Values["page"]?.ToString();

            if (string.IsNullOrEmpty(viewName))
            {
                throw new InvalidOperationException(
                    "View name could not be determined from route data. " +
                    "Fix: call WithView(\"YourPageOrPartialName\", model) (or WithView(\"~/Pages/.../YourPageOrPartialName.cshtml\", model)).");
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
                    "Fix: ensure the view exists under Pages/Shared (or Views/Shared) or provide an app-relative path via WithView().",
                    viewName,
                    searched);
                throw new InvalidOperationException(
                    $"Could not find view '{viewName}'. Searched: {searched}. " +
                    "Fix: ensure the view exists under Pages/Shared (or Views/Shared) or provide an app-relative path via WithView().");
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
        // Handle Delete mode specially - no view rendering needed (consistent with the other result types).
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
                var logger = context.HttpContext.RequestServices.GetService<ILogger<SwapPageResult>>();
                logger?.LogError(
                    "[Swap.Htmx] Could not find view {ViewName} for OOB swap targeting #{TargetId}. Searched: {SearchedLocations}. " +
                    "Fix: move the partial under Pages/Shared, provide an app-relative path, or configure SwapHtmxOptions.PartialViewSearchPaths.",
                    oob.ViewName,
                    oob.TargetId,
                    searchedPaths);

                throw new InvalidOperationException(
                    $"Could not find view '{oob.ViewName}' for OOB swap (target '#{oob.TargetId}'). Searched: {searchedPaths}. " +
                    "Fix: move the partial under Pages/Shared, provide an app-relative path, or configure SwapHtmxOptions.PartialViewSearchPaths.");
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

        var hash = oob.Fingerprint ? Swap.Htmx.Models.SwapOobAttributes.ComputeContentHash(html) : null;
        var oobAttrs = Swap.Htmx.Models.SwapOobAttributes.Build(oob.SwapMode, oob.Seq, hash, oob.ConditionalExists);

        // If the rendered HTML already contains hx-swap-oob, return as-is
        if (html.Contains("hx-swap-oob"))
        {
            // Partial self-declares its OOB target; still stamp data-swap-seq so the client guard applies.
            return Swap.Htmx.Models.SwapOobAttributes.InjectStampsIfMissing(html, oob.Seq, hash, oob.ConditionalExists);
        }
        
        // If the rendered HTML already has an element with the target ID, add the oob attribute to it
        var idPattern = $"id=\"{oob.TargetId}\"";
        if (html.Contains(idPattern))
        {
            // Insert hx-swap-oob attribute after the id attribute
            return html.Replace(idPattern, $"{idPattern} {oobAttrs}");
        }

        // Otherwise wrap in a div (fallback for views without the id)
        var swapOptions = context.HttpContext.RequestServices.GetService<SwapHtmxOptions>();
        if (swapOptions?.Diagnostics.WarnOnMissingOobTargets == true)
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<SwapPageResult>>();
            logger?.LogWarning(
                "[Swap.Htmx] OOB swap for target #{TargetId} rendered view {ViewName} without a matching id attribute. " +
            "Swap will be wrapped in a <div> with that id and hx-swap-oob=... . " +
                "If you intended to swap an existing element, ensure the rendered partial contains the target id or includes hx-swap-oob.",
                oob.TargetId,
                oob.ViewName);
        }
        return $"<div id=\"{oob.TargetId}\" {oobAttrs}>{html}</div>";
    }

}
