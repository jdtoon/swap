using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using Swap.Htmx.Results;
using Swap.Htmx.State;

namespace Swap.Htmx.Models;

/// <summary>
/// Swap mode for out-of-band updates.
/// </summary>
public enum SwapMode
{
    /// <summary>Replace the entire target element (outerHTML).</summary>
    OuterHTML,
    
    /// <summary>Replace the inner content of the target (innerHTML).</summary>
    InnerHTML,
    
    /// <summary>Insert content before the target element.</summary>
    BeforeBegin,
    
    /// <summary>Insert content at the start of the target's children.</summary>
    AfterBegin,
    
    /// <summary>Insert content at the end of the target's children.</summary>
    BeforeEnd,
    
    /// <summary>Insert content after the target element.</summary>
    AfterEnd,
    
    /// <summary>Delete the target element.</summary>
    Delete,
    
    /// <summary>Do nothing with the OOB swap.</summary>
    None
}

/// <summary>
/// Common CRUD operations for toast messages.
/// </summary>
public enum CrudOperation
{
    /// <summary>An item was created.</summary>
    Created,
    
    /// <summary>An item was updated.</summary>
    Updated,
    
    /// <summary>An item was deleted.</summary>
    Deleted,
    
    /// <summary>An item was saved (create or update).</summary>
    Saved,
    
    /// <summary>An item was archived.</summary>
    Archived,
    
    /// <summary>An item was restored.</summary>
    Restored,
    
    /// <summary>An item was duplicated.</summary>
    Duplicated
}

/// <summary>
/// Represents a pending out-of-band swap update.
/// </summary>
public sealed record OobSwap(
    string TargetId,
    string ViewName,
    object? Model,
    SwapMode SwapMode,
    bool ConditionalExists = false
);

/// <summary>
/// Represents a pending toast notification.
/// </summary>
public sealed record ToastNotification(
    string Message,
    ToastType Type
);

/// <summary>
/// Represents a pending HX-Trigger event.
/// </summary>
public sealed record TriggerEvent(
    string EventName,
    object? Payload
);

/// <summary>
/// Represents a pending client-side action.
/// </summary>
public sealed record ClientAction(
    string Action,
    string? Target = null,
    object? Value = null
);

/// <summary>
/// Fluent builder for constructing coordinated HTMX responses with multiple updates.
/// Replaces manual ViewData manipulation and Response.AddTrigger() calls with a clean, discoverable API.
/// Implements IResult for direct usage in Minimal APIs.
/// </summary>
public sealed class SwapResponseBuilder : IResult
{
    private string? _viewName;
    private object? _model;
    private readonly List<OobSwap> _oobSwaps = new();
    private readonly List<ToastNotification> _toasts = new();
    private readonly List<TriggerEvent> _triggers = new();
    private readonly List<ClientAction> _clientActions = new();
    private string? _redirectUrl;
    private SwapState? _state;
    private string? _stateViewName;
    
    // Store controller reference for implicit conversion
    internal Controller? Controller { get; set; }
    
    // Store PageModel reference for implicit conversion
    internal PageModel? PageModel { get; set; }

    /// <summary>
    /// Creates a new instance of SwapResponseBuilder.
    /// </summary>
    public SwapResponseBuilder() { }

    /// <summary>
    /// Creates a new instance of SwapResponseBuilder with a controller context.
    /// </summary>
    /// <param name="controller">The controller instance.</param>
    public SwapResponseBuilder(Controller controller) 
    {
        Controller = controller;
    }

    /// <summary>
    /// Creates a new instance of SwapResponseBuilder with a page model context.
    /// </summary>
    /// <param name="pageModel">The page model instance.</param>
    public SwapResponseBuilder(PageModel pageModel) 
    {
        PageModel = pageModel;
    }

    /// <summary>
    /// Sets the main view to render.
    /// </summary>
    /// <param name="viewName">The name of the view to render. If null, uses convention-based naming.</param>
    /// <param name="model">The model to pass to the view.</param>
    /// <returns>The builder for chaining.</returns>
    public SwapResponseBuilder WithView(string? viewName = null, object? model = null)
    {
        _viewName = viewName;
        _model = model;
        return this;
    }

    /// <summary>
    /// Adds an out-of-band swap to update another part of the page.
    /// </summary>
    /// <param name="targetId">The ID of the element to update.</param>
    /// <param name="viewName">The partial view to render for this target.</param>
    /// <param name="model">The model for the partial view.</param>
    /// <param name="swapMode">How to swap the content (defaults to OuterHTML).</param>
    /// <returns>The builder for chaining.</returns>
    public SwapResponseBuilder AlsoUpdate(
        string targetId, 
        string viewName, 
        object? model = null, 
        SwapMode swapMode = SwapMode.OuterHTML)
    {
        _oobSwaps.Add(new OobSwap(targetId, viewName, model, swapMode));
        return this;
    }

    /// <summary>
    /// Adds multiple out-of-band swaps for a collection of items.
    /// Useful for updating rows in a list or grid.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    /// <param name="items">The collection of items to update.</param>
    /// <param name="idSelector">Function to generate the target DOM ID for each item.</param>
    /// <param name="viewName">The partial view to render for each item.</param>
    /// <param name="swapMode">The swap mode (defaults to OuterHTML).</param>
    /// <returns>The builder for chaining.</returns>
    public SwapResponseBuilder AlsoUpdateMany<T>(
        IEnumerable<T> items,
        Func<T, string> idSelector,
        string viewName,
        SwapMode swapMode = SwapMode.OuterHTML)
    {
        foreach (var item in items)
        {
            AlsoUpdate(idSelector(item), viewName, item, swapMode);
        }
        return this;
    }

    /// <summary>
    /// Adds an out-of-band swap that only executes if the target element exists in the DOM.
    /// This is useful for optional page regions that may or may not be present.
    /// </summary>
    /// <param name="targetId">The ID of the element to update (if it exists).</param>
    /// <param name="viewName">The partial view to render for this target.</param>
    /// <param name="model">The model for the partial view.</param>
    /// <param name="swapMode">How to swap the content (defaults to OuterHTML).</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// Unlike <see cref="AlsoUpdate"/>, this method will not produce warnings or errors
    /// if the target element doesn't exist. The swap is marked as conditional and
    /// the client-side code will check for existence before attempting the swap.
    /// 
    /// Example:
    /// <code>
    /// return SwapResponse()
    ///     .WithView("_Grid", model)
    ///     .AlsoUpdateIfExists("#sidebar-stats", "_Stats", stats)  // May not exist
    ///     .Build();
    /// </code>
    /// </remarks>
    public SwapResponseBuilder AlsoUpdateIfExists(
        string targetId,
        string viewName,
        object? model = null,
        SwapMode swapMode = SwapMode.OuterHTML)
    {
        _oobSwaps.Add(new OobSwap(targetId, viewName, model, swapMode, ConditionalExists: true));
        return this;
    }

    /// <summary>
    /// Conditionally adds an out-of-band swap based on a server-side condition.
    /// The swap is only included in the response if the condition is true.
    /// </summary>
    /// <param name="condition">Whether to include this OOB swap.</param>
    /// <param name="targetId">The ID of the element to update.</param>
    /// <param name="viewName">The partial view to render for this target.</param>
    /// <param name="model">The model for the partial view.</param>
    /// <param name="swapMode">How to swap the content (defaults to OuterHTML).</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// Example:
    /// <code>
    /// var hasChart = user.HasPermission("ViewAnalytics");
    /// return SwapResponse()
    ///     .WithView("_Grid", model)
    ///     .AlsoUpdateIf(hasChart, "#analytics-chart", "_Chart", chartData)
    ///     .Build();
    /// </code>
    /// </remarks>
    public SwapResponseBuilder AlsoUpdateIf(
        bool condition,
        string targetId,
        string viewName,
        object? model = null,
        SwapMode swapMode = SwapMode.OuterHTML)
    {
        if (condition)
        {
            _oobSwaps.Add(new OobSwap(targetId, viewName, model, swapMode));
        }
        return this;
    }

    /// <summary>
    /// Adds a toast notification to display.
    /// </summary>
    /// <param name="message">The toast message.</param>
    /// <param name="type">The toast type (defaults to Info).</param>
    /// <returns>The builder for chaining.</returns>
    public SwapResponseBuilder WithToast(string message, ToastType type = ToastType.Info)
    {
        _toasts.Add(new ToastNotification(message, type));
        return this;
    }

    /// <summary>
    /// Adds a success toast notification.
    /// </summary>
    public SwapResponseBuilder WithSuccessToast(string message)
        => WithToast(message, ToastType.Success);

    /// <summary>
    /// Adds an error toast notification.
    /// </summary>
    public SwapResponseBuilder WithErrorToast(string message)
        => WithToast(message, ToastType.Error);

    /// <summary>
    /// Adds a warning toast notification.
    /// </summary>
    public SwapResponseBuilder WithWarningToast(string message)
        => WithToast(message, ToastType.Warning);

    /// <summary>
    /// Adds an info toast notification.
    /// </summary>
    public SwapResponseBuilder WithInfoToast(string message)
        => WithToast(message, ToastType.Info);

    /// <summary>
    /// Adds a success toast for common CRUD operations with consistent messaging.
    /// </summary>
    /// <param name="operation">The CRUD operation that was performed.</param>
    /// <param name="entityName">The name of the entity type (e.g., "Rate Card", "Quote").</param>
    /// <param name="itemName">Optional specific item name or identifier.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// Examples:
    /// <code>
    /// .WithCrudToast(CrudOperation.Created, "Rate Card")           // → "Rate Card created successfully"
    /// .WithCrudToast(CrudOperation.Updated, "Quote", "QT-2025-01") // → "Quote 'QT-2025-01' updated"
    /// .WithCrudToast(CrudOperation.Deleted, "Item")                // → "Item deleted"
    /// </code>
    /// </remarks>
    public SwapResponseBuilder WithCrudToast(CrudOperation operation, string entityName, string? itemName = null)
    {
        var message = FormatCrudMessage(operation, entityName, itemName);
        return WithSuccessToast(message);
    }

    /// <summary>
    /// Adds a toast for a created item.
    /// </summary>
    /// <param name="entityName">The name of the entity type.</param>
    /// <param name="itemName">Optional specific item name.</param>
    /// <returns>The builder for chaining.</returns>
    public SwapResponseBuilder WithCreatedToast(string entityName, string? itemName = null)
        => WithCrudToast(CrudOperation.Created, entityName, itemName);

    /// <summary>
    /// Adds a toast for an updated item.
    /// </summary>
    /// <param name="entityName">The name of the entity type.</param>
    /// <param name="itemName">Optional specific item name.</param>
    /// <returns>The builder for chaining.</returns>
    public SwapResponseBuilder WithUpdatedToast(string entityName, string? itemName = null)
        => WithCrudToast(CrudOperation.Updated, entityName, itemName);

    /// <summary>
    /// Adds a toast for a deleted item.
    /// </summary>
    /// <param name="entityName">The name of the entity type.</param>
    /// <param name="itemName">Optional specific item name.</param>
    /// <returns>The builder for chaining.</returns>
    public SwapResponseBuilder WithDeletedToast(string entityName, string? itemName = null)
        => WithCrudToast(CrudOperation.Deleted, entityName, itemName);

    /// <summary>
    /// Adds a toast for a saved item (create or update).
    /// </summary>
    /// <param name="entityName">The name of the entity type.</param>
    /// <param name="itemName">Optional specific item name.</param>
    /// <returns>The builder for chaining.</returns>
    public SwapResponseBuilder WithSavedToast(string entityName, string? itemName = null)
        => WithCrudToast(CrudOperation.Saved, entityName, itemName);

    private static string FormatCrudMessage(CrudOperation operation, string entityName, string? itemName)
    {
        var itemPart = string.IsNullOrEmpty(itemName) ? "" : $" '{itemName}'";
        
        return operation switch
        {
            CrudOperation.Created => $"{entityName}{itemPart} created successfully",
            CrudOperation.Updated => $"{entityName}{itemPart} updated",
            CrudOperation.Deleted => $"{entityName}{itemPart} deleted",
            CrudOperation.Saved => $"{entityName}{itemPart} saved",
            CrudOperation.Archived => $"{entityName}{itemPart} archived",
            CrudOperation.Restored => $"{entityName}{itemPart} restored",
            CrudOperation.Duplicated => $"{entityName}{itemPart} duplicated",
            _ => $"{entityName}{itemPart} {operation.ToString().ToLowerInvariant()}"
        };
    }

    /// <summary>
    /// Adds a custom HX-Trigger event with a type-safe event key.
    /// </summary>
    /// <param name="eventKey">The event key to trigger on the client.</param>
    /// <param name="payload">Optional payload data for the event.</param>
    /// <returns>The builder for chaining.</returns>
    public SwapResponseBuilder WithTrigger(EventKey eventKey, object? payload = null)
    {
        _triggers.Add(new TriggerEvent(eventKey.Name, payload));
        return this;
    }

    /// <summary>
    /// Adds a custom HX-Trigger event with a string event name.
    /// Prefer using the EventKey overload for type safety.
    /// </summary>
    /// <param name="eventName">The event name to trigger on the client.</param>
    /// <param name="payload">Optional payload data for the event.</param>
    /// <returns>The builder for chaining.</returns>
    public SwapResponseBuilder WithTrigger(string eventName, object? payload = null)
    {
        _triggers.Add(new TriggerEvent(eventName, payload));
        return this;
    }

    /// <summary>
    /// Sets a redirect URL for the response using HX-Redirect header.
    /// </summary>
    /// <param name="url">The URL to redirect to.</param>
    /// <returns>The builder for chaining.</returns>
    public SwapResponseBuilder WithRedirect(string url)
    {
        _redirectUrl = url;
        return this;
    }

    /// <summary>
    /// Adds a client-side action to execute after the swap.
    /// </summary>
    /// <param name="action">The action to perform (e.g., "focus", "reset", "scroll").</param>
    /// <param name="target">Optional target selector or element ID.</param>
    /// <param name="value">Optional value for the action.</param>
    /// <returns>The builder for chaining.</returns>
    public SwapResponseBuilder WithClientAction(string action, string? target = null, object? value = null)
    {
        _clientActions.Add(new ClientAction(action, target, value));
        return this;
    }

    /// <summary>
    /// Includes a SwapState container as an OOB swap in the response.
    /// The state will be rendered as hidden fields and swapped into the page.
    /// </summary>
    /// <param name="state">The SwapState instance to include.</param>
    /// <param name="viewName">Optional custom partial view name for rendering. Defaults to "_SwapStateContainer".</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// Usage:
    /// <code>
    /// return SwapResponse()
    ///     .WithView("_Grid", model)
    ///     .WithState(state)  // Auto-renders and swaps state container
    ///     .Build();
    /// </code>
    /// 
    /// This is equivalent to:
    /// <code>
    /// .AlsoUpdate(state.ContainerId, "_SwapStateContainer", state)
    /// </code>
    /// </remarks>
    public SwapResponseBuilder WithState(SwapState state, string? viewName = null)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _stateViewName = viewName;
        return this;
    }

    /// <summary>
    /// Gets the configured view name.
    /// </summary>
    internal string? ViewName => _viewName;

    /// <summary>
    /// Gets the configured model.
    /// </summary>
    internal object? Model => _model;

    /// <summary>
    /// Gets all configured OOB swaps.
    /// </summary>
    internal IReadOnlyList<OobSwap> OobSwaps => _oobSwaps;

    /// <summary>
    /// Gets all configured toasts.
    /// </summary>
    public IReadOnlyList<ToastNotification> Toasts => _toasts;

    /// <summary>
    /// Gets all configured triggers.
    /// </summary>
    public IReadOnlyList<TriggerEvent> Triggers => _triggers;

    /// <summary>
    /// Gets all configured client actions.
    /// </summary>
    public IReadOnlyList<ClientAction> ClientActions => _clientActions;

    /// <summary>
    /// Gets the configured SwapState to include in the response.
    /// </summary>
    internal SwapState? State => _state;

    /// <summary>
    /// Gets the custom view name for rendering state, if specified.
    /// </summary>
    internal string? StateViewName => _stateViewName;
    
    /// <summary>
    /// Gets the configured redirect URL.
    /// </summary>
    internal string? RedirectUrl => _redirectUrl;
    
    /// <summary>
    /// Builds and returns the final IActionResult.
    /// This allows explicit conversion when implicit conversion to ActionResult doesn't apply.
    /// </summary>
    public IActionResult Build()
    {
        if (Controller != null)
        {
            return new Swap.Htmx.Results.SwapActionResult(this, Controller);
        }
        
        if (PageModel != null)
        {
            return new Swap.Htmx.Results.SwapPageResult(this, PageModel);
        }
        
        throw new InvalidOperationException(
            "SwapResponseBuilder must be created through SwapController.SwapResponse() or PageModel.SwapResponse() to use Build().");
    }
    
    /// <summary>
    /// Builds and returns the final IResult for Minimal APIs.
    /// </summary>
    public IResult BuildResult()
    {
        return new Swap.Htmx.Results.SwapResult(this);
    }

    /// <summary>
    /// Executes the result operation of the action method asynchronously.
    /// This allows the builder to be returned directly from Minimal APIs.
    /// </summary>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        return new SwapResult(this).ExecuteAsync(httpContext);
    }

    /// <summary>
    /// Implicit conversion to ActionResult.
    /// Allows returning SwapResponseBuilder directly from controller actions.
    /// </summary>
    public static implicit operator ActionResult(SwapResponseBuilder builder)
    {
        if (builder.Controller != null)
        {
            return new Swap.Htmx.Results.SwapActionResult(builder, builder.Controller);
        }
        
        if (builder.PageModel != null)
        {
            return new Swap.Htmx.Results.SwapPageResult(builder, builder.PageModel);
        }
        
        throw new InvalidOperationException(
            "SwapResponseBuilder must be created through SwapController.SwapResponse() or PageModel.SwapResponse() to use implicit conversion.");
    }
}
