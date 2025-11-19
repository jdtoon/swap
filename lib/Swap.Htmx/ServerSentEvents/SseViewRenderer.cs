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

namespace Swap.Htmx.ServerSentEvents;

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

        // Create a minimal ActionContext for view rendering
        var actionContext = new ActionContext(
            httpContext,
            httpContext.GetRouteData() ?? new RouteData(),
            new ActionDescriptor()
        );

        using var writer = new StringWriter();
        
        // Find the view
        var viewResult = _viewEngine.FindView(actionContext, viewName, isMainPage: false);
        
        if (!viewResult.Success)
        {
            // Try partial view lookup
            viewResult = _viewEngine.GetView(executingFilePath: null, viewName, isMainPage: false);
        }

        if (!viewResult.Success)
        {
            throw new InvalidOperationException(
                $"Could not find view '{viewName}'. Searched locations: {string.Join(", ", viewResult.SearchedLocations ?? Array.Empty<string>())}");
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
