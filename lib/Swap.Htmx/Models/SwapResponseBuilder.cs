using Microsoft.AspNetCore.Mvc;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;

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
/// Represents a pending out-of-band swap update.
/// </summary>
public sealed record OobSwap(
    string TargetId,
    string ViewName,
    object? Model,
    SwapMode SwapMode
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
/// Fluent builder for constructing coordinated HTMX responses with multiple updates.
/// Replaces manual ViewData manipulation and Response.AddTrigger() calls with a clean, discoverable API.
/// </summary>
public sealed class SwapResponseBuilder
{
    private string? _viewName;
    private object? _model;
    private readonly List<OobSwap> _oobSwaps = new();
    private readonly List<ToastNotification> _toasts = new();
    private readonly List<TriggerEvent> _triggers = new();
    private string? _redirectUrl;
    
    // Store controller reference for implicit conversion
    internal Controller? Controller { get; set; }

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
    /// Gets the configured redirect URL.
    /// </summary>
    internal string? RedirectUrl => _redirectUrl;
    
    /// <summary>
    /// Builds and returns the final IActionResult.
    /// This allows explicit conversion when implicit conversion to ActionResult doesn't apply.
    /// </summary>
    public IActionResult Build()
    {
        if (Controller == null)
        {
            throw new InvalidOperationException(
                "SwapResponseBuilder must be created through SwapController.SwapResponse() to use Build().");
        }
        
        return new Swap.Htmx.Results.SwapActionResult(this, Controller);
    }
    
    /// <summary>
    /// Implicit conversion to ActionResult.
    /// Allows returning SwapResponseBuilder directly from controller actions.
    /// </summary>
    public static implicit operator ActionResult(SwapResponseBuilder builder)
    {
        if (builder.Controller == null)
        {
            throw new InvalidOperationException(
                "SwapResponseBuilder must be created through SwapController.SwapResponse() to use implicit conversion.");
        }
        
        return new Swap.Htmx.Results.SwapActionResult(builder, builder.Controller);
    }
}
