using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Swap.Htmx.Services;

/// <summary>
/// Default implementation of IViewRenderService.
/// Centralizes view rendering logic with consistent search paths and diagnostics.
/// </summary>
internal class ViewRenderService : IViewRenderService
{
    private readonly ICompositeViewEngine _viewEngine;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IModelMetadataProvider _metadataProvider;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly ILogger<ViewRenderService> _logger;
    private readonly SwapHtmxOptions _options;

    public ViewRenderService(
        ICompositeViewEngine viewEngine,
        IHttpContextAccessor httpContextAccessor,
        IModelMetadataProvider metadataProvider,
        ITempDataProvider tempDataProvider,
        ILogger<ViewRenderService> logger,
        IOptions<SwapHtmxOptions> options)
    {
        _viewEngine = viewEngine;
        _httpContextAccessor = httpContextAccessor;
        _metadataProvider = metadataProvider;
        _tempDataProvider = tempDataProvider;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<string> RenderPartialToStringAsync<TModel>(string viewName, TModel model, Controller? controller = null)
    {
        if (string.IsNullOrEmpty(viewName))
        {
            if (controller != null)
            {
                viewName = controller.ControllerContext.ActionDescriptor.ActionName;
            }
            else
            {
                throw new ArgumentException("View name is required when no controller context is available.", nameof(viewName));
            }
        }

        var httpContext = controller?.HttpContext ?? _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new InvalidOperationException("HttpContext is not available. Ensure the service is called within an HTTP request context.");
        }

        // Create action context from controller if available, otherwise build minimal context
        ActionContext actionContext;
        ViewDataDictionary viewData;
        ITempDataDictionary tempData;

        if (controller != null)
        {
            actionContext = controller.ControllerContext;
            viewData = new ViewDataDictionary(_metadataProvider, controller.ModelState)
            {
                Model = model
            };
            tempData = controller.TempData;
        }
        else
        {
            var routeData = httpContext.GetRouteData() ?? new RouteData();
            var actionDescriptor = new ActionDescriptor();
            actionContext = new ActionContext(httpContext, routeData, actionDescriptor);
            viewData = new ViewDataDictionary(_metadataProvider, new ModelStateDictionary())
            {
                Model = model
            };
            tempData = new TempDataDictionary(httpContext, _tempDataProvider);
        }

        using var writer = new StringWriter();
        var searchedLocations = new List<string>();

        // Search strategy: FindView → GetView → fallback search paths
        var viewResult = _viewEngine.FindView(actionContext, viewName, isMainPage: false);
        searchedLocations.AddRange(viewResult.SearchedLocations ?? Array.Empty<string>());

        if (!viewResult.Success)
        {
            viewResult = _viewEngine.GetView(executingFilePath: null, viewName, isMainPage: false);
            searchedLocations.AddRange(viewResult.SearchedLocations ?? Array.Empty<string>());
        }

        if (!viewResult.Success)
        {
            var searchPaths = _options.PartialViewSearchPaths ?? new List<string> { "Shared" };
            var viewFile = viewName.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase)
                ? viewName
                : $"{viewName}.cshtml";

            foreach (var path in searchPaths)
            {
                var location = $"~/Views/{path}/{viewFile}";
                searchedLocations.Add(location);
                viewResult = _viewEngine.GetView(executingFilePath: null, viewPath: location, isMainPage: false);
                if (viewResult.Success)
                {
                    break;
                }
            }
        }

        if (!viewResult.Success)
        {
            var searched = string.Join(", ", searchedLocations.Distinct());
            _logger.LogError(
                "[Swap.Htmx] Could not find view {ViewName}. Searched: {SearchedLocations}. " +
                "Fix: ensure the view exists under Views/Shared (or the current controller folder), provide an app-relative path (~/Views/.../View.cshtml), " +
                "or configure SwapHtmxOptions.PartialViewSearchPaths.",
                viewName,
                searched);

            throw new InvalidOperationException(
                $"Could not find view '{viewName}'. Searched: {searched}. " +
                "Fix: ensure the view exists under Views/Shared (or the current controller folder), provide an app-relative path (~/Views/.../View.cshtml), " +
                "or configure SwapHtmxOptions.PartialViewSearchPaths.");
        }

        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewData,
            tempData,
            writer,
            new HtmlHelperOptions()
        );

        await viewResult.View.RenderAsync(viewContext);
        return writer.ToString();
    }
}
