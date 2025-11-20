using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using Swap.Htmx.Results;
using Swap.Htmx.ServerSentEvents;

namespace Swap.Htmx;

/// <summary>
/// Base controller for HTMX-enabled applications.
/// Automatically handles page vs partial rendering based on HX-Request header.
/// </summary>
public abstract class SwapController : Controller
{
    /// <summary>
    /// Gets the session ID, ensuring the session cookie is sent to the client.
    /// Call this instead of HttpContext.Session.Id to avoid session persistence issues.
    /// ASP.NET Core doesn't send the session cookie until you write a value to the session.
    /// </summary>
    /// <returns>The session ID that will persist across requests.</returns>
    /// <example>
    /// <code>
    /// public class CartController : SwapController
    /// {
    ///     private string SessionId => GetOrInitializeSessionId();
    ///     
    ///     public IActionResult Index()
    ///     {
    ///         var cart = _cartService.GetCart(SessionId);
    ///         return SwapView(cart);
    ///     }
    /// }
    /// </code>
    /// </example>
    protected string GetOrInitializeSessionId()
    {
        const string InitKey = "_swap_session_initialized";
        
        // Ensure session cookie is sent by writing a value if not already present
        if (!HttpContext.Session.Keys.Contains(InitKey))
        {
            HttpContext.Session.SetString(InitKey, DateTime.UtcNow.ToString("O"));
        }
        
        return HttpContext.Session.Id;
    }

    /// <summary>
    /// Returns a view result that automatically chooses between full page or partial view
    /// based on whether the request is an HTMX request (HX-Request header present).
    /// </summary>
    /// <param name="viewName">The name of the view to render. If null, uses conventional view name.</param>
    /// <param name="model">The model to pass to the view.</param>
    /// <returns>
    /// - For HTMX requests (HX-Request header present): Returns partial view
    /// - For normal requests (initial page load, refresh): Returns full view with layout
    /// </returns>
    /// <example>
    /// <code>
    /// public async Task&lt;IActionResult&gt; Index()
    /// {
    ///     var articles = await _context.Articles.ToListAsync();
    ///     return SwapView(articles);
    /// }
    /// </code>
    /// </example>
    protected IActionResult SwapView(object? model = null)
    {
        return SwapView(viewName: null, model: model);
    }

    /// <summary>
    /// Returns a view result that automatically chooses between full page or partial view
    /// based on whether the request is an HTMX request (HX-Request header present).
    /// </summary>
    /// <param name="viewName">The name of the view to render. If null, uses conventional view name.</param>
    /// <param name="model">The model to pass to the view.</param>
    /// <returns>
    /// - For HTMX requests (HX-Request header present): Returns partial view
    /// - For normal requests (initial page load, refresh): Returns full view with layout
    /// </returns>
    /// <example>
    /// <code>
    /// public async Task&lt;IActionResult&gt; Details(int id)
    /// {
    ///     var article = await _context.Articles.FindAsync(id);
    ///     return SwapView("Details", article);
    /// }
    /// </code>
    /// </example>
    protected IActionResult SwapView(string? viewName, object? model = null)
    {
        // Content varies based on HX-Request; communicate this for caches/CDNs
        Response.EnsureVaryHxRequest();

        bool isHtmxRequest = Request.IsHtmxRequest();

        if (isHtmxRequest)
        {
            // HTMX request - return partial view without layout
            return PartialView(viewName, model);
        }
        else
        {
            // Normal request (initial load or refresh) - return full view with layout
            return View(viewName, model);
        }
    }

    /// <summary>
    /// Creates a fluent response builder for coordinating multiple updates in a single response.
    /// This is the recommended approach when you need to combine view rendering, OOB swaps,
    /// toasts, and custom triggers.
    /// </summary>
    /// <returns>A fluent builder for constructing coordinated HTMX responses.</returns>
    /// <example>
    /// <code>
    /// public IActionResult AddToCart(int productId)
    /// {
    ///     _cart.Add(productId);
    ///     
    ///     return SwapResponse()
    ///         .WithView("_ProductAdded")
    ///         .AlsoUpdate("cart-count", "_CartCount", _cart.Count)
    ///         .AlsoUpdate("cart-total", "_CartTotal", _cart.Total)
    ///         .WithSuccessToast("Added to cart!");
    /// }
    /// </code>
    /// </example>
    protected SwapResponseBuilder SwapResponse()
    {
        var builder = new SwapResponseBuilder();
        builder.Controller = this;
        return builder;
    }

    /// <summary>
    /// Returns an out-of-band (OOB) partial view with the hx-swap-oob attribute.
    /// Use this to update multiple parts of the page in a single response.
    /// </summary>
    /// <param name="targetId">The ID of the element to swap into (used in hx-swap-oob attribute).</param>
    /// <param name="viewName">The name of the view to render. If null, uses conventional view name.</param>
    /// <param name="model">The model to pass to the view.</param>
    /// <param name="swapStrategy">The swap strategy (defaults to "true" which means "outerHTML"). Can be "innerHTML", "beforebegin", "afterbegin", "beforeend", "afterend", "delete", "none".</param>
    /// <returns>A partial view with hx-swap-oob attribute for out-of-band swapping.</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;IActionResult&gt; UpdateCartAndTotal(int productId)
    /// {
    ///     // Update main content
    ///     var mainContent = SwapView("ProductAdded");
    ///     
    ///     // Also update cart total out-of-band
    ///     var cartTotal = await _cartService.GetTotalAsync();
    ///     ViewData["OobCartTotal"] = SwapOobView("cart-total", "_CartTotal", cartTotal);
    ///     
    ///     return mainContent;
    /// }
    /// </code>
    /// </example>
    protected IActionResult SwapOobView(string targetId, string? viewName = null, object? model = null, string swapStrategy = "true")
    {
        ViewData["HxSwapOob"] = swapStrategy;
        ViewData["OobTargetId"] = targetId;
        return PartialView(viewName, model);
    }

    /// <summary>
    /// Redirects to another action method while preserving ViewData setup from the target action.
    /// Useful for POST-Redirect-GET pattern where the POST action needs to show the same view
    /// as the GET action with all its ViewBag/ViewData populated correctly.
    /// </summary>
    /// <param name="actionName">The name of the action method to invoke.</param>
    /// <param name="routeValues">Route values to pass to the target action.</param>
    /// <returns>The result from executing the target action.</returns>
    /// <example>
    /// <code>
    /// [HttpPost]
    /// public async Task&lt;IActionResult&gt; Create(CreateDto dto)
    /// {
    ///     var item = await _service.CreateAsync(dto);
    ///     // Calls Details(item.Id) action, which populates all ViewBag data
    ///     return SwapRedirectToAction(nameof(Details), new { id = item.Id });
    /// }
    /// </code>
    /// </example>
    protected async Task<IActionResult> SwapRedirectToAction(string actionName, object? routeValues = null)
    {
        // Get the target action descriptor
        var actionDescriptor = ControllerContext.ActionDescriptor;
        var controllerName = actionDescriptor.ControllerName;

        // Invoke the target action using reflection
        var method = GetType().GetMethod(actionName);
        if (method == null)
        {
            throw new InvalidOperationException($"Action method '{actionName}' not found on controller '{controllerName}'");
        }

        // Build parameters from route values
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];

        if (routeValues != null)
        {
            var routeDict = new RouteValueDictionary(routeValues);
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                if (routeDict.TryGetValue(param.Name!, out var value))
                {
                    args[i] = Convert.ChangeType(value, param.ParameterType);
                }
            }
        }

        // Invoke the action method
        var result = method.Invoke(this, args);

        // Handle async results
        if (result is Task<IActionResult> taskResult)
        {
            return await taskResult;
        }
        else if (result is IActionResult actionResult)
        {
            return actionResult;
        }
        else
        {
            throw new InvalidOperationException($"Action method '{actionName}' did not return IActionResult");
        }
    }

    /// <summary>
    /// Creates a Server-Sent Events (SSE) connection for streaming real-time HTML updates to the client.
    /// Use with HTMX's hx-sse attribute to receive live updates.
    /// </summary>
    /// <param name="handler">The async function that streams events using the ServerSentEventStream.</param>
    /// <returns>An IActionResult that establishes and maintains an SSE connection.</returns>
    /// <example>
    /// <code>
    /// public IActionResult LiveFeed()
    /// {
    ///     return ServerSentEvents(async (stream, ct) =>
    ///     {
    ///         // Send initial state
    ///         await stream.SendEventAsync("initial", "&lt;div&gt;Connected&lt;/div&gt;");
    ///         
    ///         // Stream updates periodically
    ///         while (!ct.IsCancellationRequested)
    ///         {
    ///             await Task.Delay(1000, ct);
    ///             var html = $"&lt;div&gt;Update at {DateTime.Now}&lt;/div&gt;";
    ///             await stream.SendEventAsync("update", html);
    ///         }
    ///     });
    /// }
    /// </code>
    /// </example>
    protected IActionResult ServerSentEvents(Func<ServerSentEventStream, CancellationToken, Task> handler)
    {
        var logger = HttpContext.RequestServices.GetService<ILogger<ServerSentEventsResult>>();
        return new ServerSentEventsResult(handler, logger);
    }

    /// <summary>
    /// Creates an enhanced SSE connection with connection management, rooms, and event filtering.
    /// This version integrates with the SSE event bridge for automatic event-driven broadcasting.
    /// </summary>
    /// <param name="handler">The async function that configures and maintains the SSE connection.</param>
    /// <returns>An enhanced SSE result with connection registry integration.</returns>
    /// <example>
    /// <code>
    /// public IActionResult EnhancedLiveFeed()
    /// {
    ///     return ServerSentEvents(async (connection, ct) =>
    ///     {
    ///         await connection
    ///             .WithAuthentication()
    ///             .WithRooms("dashboard", $"user-{UserId}")
    ///             .WithEvents("task-updated", "notification")
    ///             .WithInitialState("initial", await RenderPartialToStringAsync("_Dashboard", model))
    ///             .KeepAlive(cancellationToken: ct);
    ///     });
    /// }
    /// </code>
    /// </example>
    protected IActionResult ServerSentEvents(Func<SseConnectionBuilder, CancellationToken, Task> handler)
    {
        // Store controller reference for partial view rendering
        HttpContext.Items["SwapController"] = this;
        return new EnhancedServerSentEventsResult(handler);
    }

    /// <summary>
    /// Renders a partial view to a string for use in SSE or other scenarios.
    /// </summary>
    public async Task<string> RenderPartialToStringAsync<TModel>(string viewName, TModel model)
    {
        if (string.IsNullOrEmpty(viewName))
            viewName = ControllerContext.ActionDescriptor.ActionName;

        using var writer = new StringWriter();
        var viewEngine = HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
        var viewResult = viewEngine!.FindView(ControllerContext, viewName, false);

        if (!viewResult.Success)
        {
            throw new InvalidOperationException($"Could not find view '{viewName}'");
        }

        var metadataProvider = HttpContext.RequestServices.GetService(typeof(IModelMetadataProvider)) as IModelMetadataProvider;
        var viewData = new ViewDataDictionary(metadataProvider!, new ModelStateDictionary())
        {
            Model = model
        };

        var viewContext = new ViewContext(
            ControllerContext,
            viewResult.View,
            viewData,
            TempData,
            writer,
            new HtmlHelperOptions()
        );

        await viewResult.View.RenderAsync(viewContext);
        return writer.ToString();
    }

    /// <summary>
    /// Renders an out-of-band swap as HTML string with hx-swap-oob attribute.
    /// Used internally by the fluent OOB API.
    /// </summary>
    protected async Task<string> RenderOobSwapAsync(string targetId, string viewName, object? model, string swapMode = "true")
    {
        var partialHtml = await RenderPartialToStringAsync(viewName, model);

        // Wrap in div with hx-swap-oob attribute if not already present
        if (!partialHtml.Contains("hx-swap-oob"))
        {
            return $"<div id=\"{targetId}\" hx-swap-oob=\"{swapMode}\">{partialHtml}</div>";
        }

        return partialHtml;
    }

    /// <summary>
    /// Executes a configured event chain and returns the coordinated response.
    /// This is the event-driven approach: you emit an event, and all configured UI updates happen automatically.
    /// </summary>
    /// <param name="eventKey">The event to trigger.</param>
    /// <param name="payload">Optional payload data to include with the event.</param>
    /// <returns>
    /// A SwapResponseBuilder with all configured partials, toasts, and triggers from the event chain,
    /// or an empty builder if no chain is configured.
    /// </returns>
    /// <example>
    /// <code>
    /// // In Startup.cs - configure the event chain
    /// builder.Services.AddSwapHtmx(events =>
    /// {
    ///     events.When(ProductEvents.Created)
    ///           .RefreshPartial("product-list", "_ProductList", ctx => GetProducts())
    ///           .RefreshPartial("product-count", "_ProductCount", ctx => GetCount())
    ///           .SuccessToast("Product created!");
    /// });
    /// 
    /// // In controller - just emit the event
    /// public async Task&lt;IActionResult&gt; CreateProduct(Product product)
    /// {
    ///     await _db.Products.AddAsync(product);
    ///     await _db.SaveChangesAsync();
    ///     
    ///     // All UI updates happen automatically based on configuration
    ///     return SwapEvent(ProductEvents.Created);
    /// }
    /// </code>
    /// </example>
    protected SwapResponseBuilder SwapEvent(EventKey eventKey, object? payload = null)
    {
        var logger = HttpContext?.RequestServices?.GetService<ILogger<SwapController>>();
        
        Dev.SwapDevLogger.LogSwapEvent(logger, eventKey.Name, $"Payload: {payload?.GetType().Name ?? "null"}");
        
        var executor = HttpContext?.RequestServices?.GetService(typeof(Events.IEventChainExecutor)) 
            as Events.IEventChainExecutor;

        Dev.SwapDevLogger.LogExecutor(logger, $"Executor found: {executor != null}");

        if (executor != null && HttpContext != null)
        {
            var result = executor.Execute(eventKey, HttpContext, this, payload);
            Dev.SwapDevLogger.LogExecutor(logger, $"Executor returned: {result != null}");
            
            if (result != null)
            {
                // If payload provided, add it as a trigger
                if (payload != null)
                {
                    result.WithTrigger(eventKey, payload);
                }
                return result;
            }
        }

        // No chain configured or no executor - return empty builder
        var builder = new SwapResponseBuilder();
        builder.Controller = this;
        
        // Still include the trigger event with payload if provided
        if (payload != null)
        {
            builder.WithTrigger(eventKey, payload);
        }
        
        return builder;
    }

    /// <summary>
    /// Asynchronously executes a configured event chain and returns the coordinated response.
    /// Use this when your event chain includes async model factories (e.g., database queries).
    /// </summary>
    /// <param name="eventKey">The event to trigger.</param>
    /// <param name="payload">Optional payload data to include with the event.</param>
    /// <returns>
    /// A SwapResponseBuilder with all configured partials, toasts, and triggers from the event chain,
    /// or an empty builder if no chain is configured.
    /// </returns>
    protected async Task<SwapResponseBuilder> SwapEventAsync(EventKey eventKey, object? payload = null)
    {
        var logger = HttpContext?.RequestServices?.GetService<ILogger<SwapController>>();
        
        Dev.SwapDevLogger.LogSwapEvent(logger, eventKey.Name, $"Payload: {payload?.GetType().Name ?? "null"} (Async)");
        
        var executor = HttpContext?.RequestServices?.GetService(typeof(Events.IEventChainExecutor)) 
            as Events.IEventChainExecutor;

        if (executor != null && HttpContext != null)
        {
            var result = await executor.ExecuteAsync(eventKey, HttpContext, this, payload);
            
            if (result != null)
            {
                // If payload provided, add it as a trigger
                if (payload != null)
                {
                    result.WithTrigger(eventKey, payload);
                }
                return result;
            }
        }

        // No chain configured or no executor - return empty builder
        var builder = new SwapResponseBuilder();
        builder.Controller = this;
        
        // Still include the trigger event with payload if provided
        if (payload != null)
        {
            builder.WithTrigger(eventKey, payload);
        }
        
        return builder;
    }
}

