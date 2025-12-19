using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Swap.Htmx.Realtime;

/// <summary>
/// Service for rendering Razor partial views during SSE broadcasts.
/// Unlike SwapController.RenderPartialToStringAsync, this doesn't require
/// an active controller instance or HTTP request context.
/// </summary>
public interface ISseViewRenderer
{
    /// <summary>
    /// Renders a partial view to HTML string for SSE broadcast.
    /// </summary>
    /// <param name="viewName">The name of the partial view to render.</param>
    /// <param name="model">The model to pass to the view.</param>
    /// <returns>The rendered HTML string.</returns>
    Task<string> RenderPartialAsync<TModel>(string viewName, TModel model);
}

public class SseViewRenderer : ISseViewRenderer
{
    private readonly IRazorViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SseViewRenderer(
        IRazorViewEngine viewEngine,
        ITempDataProvider tempDataProvider,
        IServiceProvider serviceProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
        _serviceProvider = serviceProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> RenderPartialAsync<TModel>(string viewName, TModel model)
    {
        // Get or create an HttpContext for view rendering
        var httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };

        // Create a RouteData with controller context to help view engine find views
        var routeData = httpContext.GetRouteData() ?? new RouteData();
        
        // If view name doesn't start with ~/ or /, try to infer controller from view path
        // For views like "Stats", we'll search in common locations
        // The view engine will search: Views/{Controller}/{ViewName}, Views/Shared/{ViewName}
        
        // Create ActionDescriptor with controller name to help view engine
        var actionDescriptor = new ActionDescriptor
        {
            RouteValues = new Dictionary<string, string?>()
        };

        // Create ActionContext for view rendering
        var actionContext = new ActionContext(
            httpContext,
            routeData,
            actionDescriptor
        );

        using var writer = new StringWriter();
        
        // Try multiple search strategies for the view
        ViewEngineResult viewResult;
        
        // 1. Try as partial view name (searches Views/Shared and current controller)
        viewResult = _viewEngine.FindView(actionContext, viewName, isMainPage: false);
        
        if (!viewResult.Success)
        {
            // 2. Try with ~ prefix for app-relative path
            viewResult = _viewEngine.GetView(executingFilePath: null, viewName, isMainPage: false);
        }

        if (!viewResult.Success)
        {
            // 3. Try common controller locations (Dashboard, Tasks, etc.)
            var commonControllers = new[] { "Dashboard", "Tasks", "Projects", "Notifications", "Shared" };
            foreach (var controller in commonControllers)
            {
                var controllerActionContext = new ActionContext(
                    httpContext,
                    routeData,
                    new ActionDescriptor
                    {
                        RouteValues = new Dictionary<string, string?>
                        {
                            ["controller"] = controller
                        }
                    }
                );
                
                viewResult = _viewEngine.FindView(controllerActionContext, viewName, isMainPage: false);
                if (viewResult.Success)
                {
                    actionContext = controllerActionContext;
                    break;
                }
            }
        }

        if (!viewResult.Success)
        {
            var searched = string.Join(", ", viewResult.SearchedLocations ?? Array.Empty<string>());
            throw new InvalidOperationException(
                $"Could not find view '{viewName}'. Searched locations: {searched}. " +
                "Fix: ensure the view exists under Views/Shared (or the appropriate controller folder), " +
                "or pass an app-relative path like '~/Views/Shared/MyPartial.cshtml'.");
        }

        // Create ViewData with the model
        var metadataProvider = httpContext.RequestServices.GetRequiredService<IModelMetadataProvider>();
        var viewData = new ViewDataDictionary<TModel>(metadataProvider, new ModelStateDictionary())
        {
            Model = model
        };

        var tempData = new TempDataDictionary(httpContext, _tempDataProvider);

        // Create ViewContext and render
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
