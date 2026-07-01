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
using Swap.Htmx.Services;

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
        var userContext = HttpContext.RequestServices.GetService<ISwapUserContext>();
        if (userContext != null)
        {
            return userContext.GetSessionId();
        }

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
        var service = HttpContext?.RequestServices?.GetService<ISwapEventService>();
        if (service != null)
        {
            return service.Response(this);
        }
        
        // Lean fallback: create builder without duplicating service logic
        // This path is primarily for tests; production apps should use AddSwapHtmx()
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
    [Obsolete("SwapRedirectToAction is a server-side forward helper (not a real redirect). It bypasses MVC model binding/filters and is easy to misuse. Prefer returning the target view directly, or use SwapRedirect()/WithNavigation() for client navigation.")]
    protected async Task<IActionResult> SwapRedirectToAction(string actionName, object? routeValues = null)
    {
        if (string.IsNullOrWhiteSpace(actionName))
            throw new ArgumentException("Action name is required.", nameof(actionName));

        var controllerName = (ControllerContext?.ActionDescriptor?.ControllerName) ?? GetType().Name;

        var candidates = GetType()
            .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            .Where(m => string.Equals(m.Name, actionName, StringComparison.Ordinal))
            .ToArray();

        if (candidates.Length == 0)
        {
            throw new InvalidOperationException($"Action method '{actionName}' not found on controller '{controllerName}'.");
        }

        var routeDict = routeValues != null ? new RouteValueDictionary(routeValues) : new RouteValueDictionary();
        var viable = candidates
            .Select(m =>
            {
                var built = TryBuildArgs(m, routeDict);
                return (Method: m, Success: built.Success, Args: built.Args);
            })
            .Where(x => x.Success)
            .ToArray();

        if (viable.Length == 0)
        {
            var available = string.Join(", ", candidates.Select(m => $"{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name))})"));
            throw new InvalidOperationException(
                $"No overload of '{actionName}' on '{controllerName}' matched the provided route values. " +
                $"Avoid overloads for actions used with SwapRedirectToAction. Available: {available}");
        }

        if (viable.Length > 1)
        {
            throw new InvalidOperationException(
                $"Ambiguous action '{actionName}' on '{controllerName}'. Multiple overloads match the provided route values. " +
                "Avoid overloaded action method names when using SwapRedirectToAction.");
        }

        var method = viable[0].Method;
        var args = viable[0].Args;
        var result = method.Invoke(this, args);

        if (result is IActionResult actionResult)
            return actionResult;

        if (result is Task<IActionResult> taskIActionResult)
            return await taskIActionResult;

        if (result is Task task)
        {
            await task;
            var resultProperty = task.GetType().GetProperty("Result");
            if (resultProperty?.GetValue(task) is IActionResult awaitedResult)
                return awaitedResult;
        }

        throw new InvalidOperationException($"Action method '{actionName}' did not return IActionResult.");

        (bool Success, object?[] Args) TryBuildArgs(System.Reflection.MethodInfo methodInfo, RouteValueDictionary values)
        {
            var parameters = methodInfo.GetParameters();
            var built = new object?[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var paramType = param.ParameterType;

                if (paramType == typeof(CancellationToken))
                {
                    built[i] = HttpContext?.RequestAborted ?? CancellationToken.None;
                    continue;
                }

                if (param.Name != null && values.TryGetValue(param.Name, out var raw))
                {
                    if (!TryConvert(raw, paramType, out var converted))
                        return (false, Array.Empty<object?>());

                    built[i] = converted;
                    continue;
                }

                if (param.HasDefaultValue)
                {
                    built[i] = param.DefaultValue;
                    continue;
                }

                if (!paramType.IsValueType || Nullable.GetUnderlyingType(paramType) != null)
                {
                    built[i] = null;
                    continue;
                }

                // Required non-nullable value type with no provided value.
                return (false, Array.Empty<object?>());
            }

            return (true, built);
        }

        static bool TryConvert(object? raw, Type targetType, out object? converted)
        {
            converted = null;
            if (raw is null)
            {
                if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                    return true;
                return false;
            }

            var nonNullableTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (nonNullableTarget.IsInstanceOfType(raw))
            {
                converted = raw;
                return true;
            }

            if (nonNullableTarget == typeof(string))
            {
                converted = Convert.ToString(raw);
                return true;
            }

            if (nonNullableTarget.IsEnum)
            {
                if (raw is string s)
                {
                    if (Enum.TryParse(nonNullableTarget, s, ignoreCase: true, out var enumVal))
                    {
                        converted = enumVal;
                        return true;
                    }
                    return false;
                }

                try
                {
                    converted = Enum.ToObject(nonNullableTarget, raw);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            if (nonNullableTarget == typeof(Guid))
            {
                if (raw is Guid g)
                {
                    converted = g;
                    return true;
                }

                if (raw is string gs && Guid.TryParse(gs, out var parsed))
                {
                    converted = parsed;
                    return true;
                }

                return false;
            }

            try
            {
                converted = Convert.ChangeType(raw, nonNullableTarget);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Renders a partial view to a string for use in SSE or other scenarios.
    /// </summary>
    public async Task<string> RenderPartialToStringAsync<TModel>(string viewName, TModel model)
    {
        var viewRenderer = HttpContext.RequestServices.GetService<IViewRenderService>();
        if (viewRenderer == null)
        {
            throw new InvalidOperationException(
                "Swap.Htmx is not configured for this application. " +
                "Fix: call builder.Services.AddSwapHtmx(...) in Program.cs (and app.UseSwapHtmx() in the middleware pipeline)."
            );
        }
        
        return await viewRenderer.RenderPartialToStringAsync(viewName, model, this);

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
        var service = HttpContext?.RequestServices?.GetService<ISwapEventService>();
        if (service == null)
        {
            throw new InvalidOperationException(
                "Swap.Htmx is not configured for this application. " +
                "Fix: call builder.Services.AddSwapHtmx(...) in Program.cs (and app.UseSwapHtmx() in the middleware pipeline)."
            );
        }

        return service.Event(eventKey, this, payload);
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
        var service = HttpContext?.RequestServices?.GetService<ISwapEventService>();
        if (service == null)
        {
            throw new InvalidOperationException(
                "Swap.Htmx is not configured for this application. " +
                "Fix: call builder.Services.AddSwapHtmx(...) in Program.cs (and app.UseSwapHtmx() in the middleware pipeline)."
            );
        }

        return await service.EventAsync(eventKey, this, payload);
    }
}

