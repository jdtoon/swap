using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Swap.Htmx.WebSockets;

/// <summary>
/// Extension methods for WebSocket handlers to support Razor view rendering.
/// </summary>
public static class WebSocketHandlerExtensions
{
    /// <summary>
    /// Renders a Razor partial view to a string for sending over WebSocket.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="handler">The WebSocket handler.</param>
    /// <param name="viewName">The name of the partial view.</param>
    /// <param name="model">The model to pass to the view.</param>
    /// <returns>The rendered HTML as a string.</returns>
    public static async Task<string> RenderPartialToStringAsync<TModel>(
        this SwapWebSocketHandler handler,
        string viewName,
        TModel model)
    {
        var httpContext = handler.HttpContext 
            ?? throw new InvalidOperationException("HttpContext is not available");

        var viewEngine = httpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
        
        // Create minimal action context for view resolution
        var routeData = new RouteData();
        routeData.Values["controller"] = "Test"; // Default controller for view discovery
        var actionContext = new ActionContext(httpContext, routeData, new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

        var viewResult = viewEngine.FindView(actionContext, viewName, isMainPage: false);
        if (!viewResult.Success)
        {
            throw new InvalidOperationException($"Partial view '{viewName}' not found. Searched locations: {string.Join(", ", viewResult.SearchedLocations ?? Array.Empty<string>())}");
        }

        using var sw = new StringWriter();
        var viewDataDictionary = new ViewDataDictionary<TModel>(
            new EmptyModelMetadataProvider(),
            new ModelStateDictionary())
        {
            Model = model
        };

        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewDataDictionary,
            new TempDataDictionary(actionContext.HttpContext, httpContext.RequestServices.GetRequiredService<ITempDataProvider>()),
            sw,
            new HtmlHelperOptions()
        );

        await viewResult.View.RenderAsync(viewContext);
        return sw.ToString();
    }
}
