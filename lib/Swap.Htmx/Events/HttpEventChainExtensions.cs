using Microsoft.AspNetCore.Http;
using Swap.Htmx.Extensions;
using Swap.Htmx.Models;

namespace Swap.Htmx.Events;

/// <summary>
/// Represents a partial view to render when an event is triggered.
/// </summary>
public sealed record EventPartialHandler(
    string TargetId,
    string ViewName,
    Func<HttpContext, object?>? ModelFactory,
    Func<HttpContext, object?, object?>? ModelFactoryWithPayload,
    Func<HttpContext, Task<object?>>? ModelFactoryAsync,
    Func<HttpContext, object?, Task<object?>>? ModelFactoryWithPayloadAsync,
    SwapMode SwapMode = SwapMode.OuterHTML
);

/// <summary>
/// Represents a toast to show when an event is triggered.
/// </summary>
public sealed record EventToastHandler(
    string Message,
    ToastType Type
);

/// <summary>
/// Represents a redirect to perform when an event is triggered.
/// </summary>
public sealed record EventRedirectHandler(
    string Url
);

/// <summary>
/// Configuration for what should happen when a specific event is triggered.
/// </summary>
public sealed class EventChainConfiguration
{
    internal List<EventPartialHandler> Partials { get; } = new();
    internal List<EventToastHandler> Toasts { get; } = new();
    internal List<EventKey> TriggerEvents { get; } = new();
    internal EventRedirectHandler? Redirect { get; set; }
}

/// <summary>
/// Extension methods for configuring event-driven HTTP responses.
/// </summary>
public static class HttpEventChainExtensions
{
    private static readonly string EventChainConfigKey = "Swap.EventChainConfigs";

    /// <summary>
    /// Configures what should happen when a specific event is triggered in HTTP responses.
    /// This enables event-driven UI updates without manually coordinating each response.
    /// </summary>
    /// <param name="options">The event bus options.</param>
    /// <param name="eventKey">The event that triggers the configured actions.</param>
    /// <returns>A fluent builder for configuring the event chain.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddSwapHtmx(events =>
    /// {
    ///     events.When(ProductEvents.Created)
    ///           .RefreshPartial("product-list", "_ProductList", ctx => GetProducts())
    ///           .RefreshPartial("product-count", "_ProductCount", ctx => GetCount())
    ///           .Toast("Product created!", ToastType.Success);
    /// });
    /// </code>
    /// </example>
    public static HttpEventChainBuilder When(this SwapEventBusOptions options, EventKey eventKey)
    {
        return new HttpEventChainBuilder(options, eventKey);
    }

    internal static Dictionary<string, EventChainConfiguration> GetEventChainConfigs(this SwapEventBusOptions options)
    {
        // Store configurations in a property bag on the options object
        if (!options.Chains.ContainsKey(EventChainConfigKey))
        {
            options.Chains[EventChainConfigKey] = new HashSet<string>();
        }
        
        // Use a static dictionary keyed by options instance
        return EventChainConfigStore.GetOrCreate(options);
    }
}

/// <summary>
/// Internal storage for event chain configurations.
/// Uses ConditionalWeakTable to avoid memory leaks.
/// </summary>
internal static class EventChainConfigStore
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<SwapEventBusOptions, Dictionary<string, EventChainConfiguration>> _store = new();

    public static Dictionary<string, EventChainConfiguration> GetOrCreate(SwapEventBusOptions options)
    {
        return _store.GetOrCreateValue(options);
    }
}

/// <summary>
/// Fluent builder for configuring HTTP event chains.
/// </summary>
public sealed class HttpEventChainBuilder
{
    private readonly SwapEventBusOptions _options;
    private readonly EventKey _eventKey;
    private readonly EventChainConfiguration _config;

    internal HttpEventChainBuilder(SwapEventBusOptions options, EventKey eventKey)
    {
        _options = options;
        _eventKey = eventKey;
        
        var configs = options.GetEventChainConfigs();
        if (!configs.TryGetValue(eventKey.Name, out _config!))
        {
            _config = new EventChainConfiguration();
            configs[eventKey.Name] = _config;
        }
    }

    /// <summary>
    /// Adds a partial view refresh to the event chain.
    /// When the event is triggered, this partial will be rendered and sent as an OOB swap.
    /// </summary>
    /// <param name="targetId">The ID of the element to update.</param>
    /// <param name="viewName">The partial view to render.</param>
    /// <param name="modelFactory">Optional factory function to create the model from HttpContext.</param>
    /// <param name="swapMode">How to swap the content (defaults to OuterHTML).</param>
    /// <returns>The builder for chaining.</returns>
    public HttpEventChainBuilder RefreshPartial(
        string targetId,
        string viewName,
        Func<HttpContext, object?>? modelFactory = null,
        SwapMode swapMode = SwapMode.OuterHTML)
    {
        _config.Partials.Add(new EventPartialHandler(targetId, viewName, modelFactory, null, null, null, swapMode));
        return this;
    }

    /// <summary>
    /// Adds a partial view refresh with access to the event payload.
    /// When the event is triggered, the model factory receives both HttpContext and the event payload.
    /// This avoids re-fetching data that was already available when the event was published.
    /// </summary>
    /// <param name="targetId">The ID of the element to update.</param>
    /// <param name="viewName">The partial view to render.</param>
    /// <param name="modelFactory">Factory function receiving HttpContext and event payload.</param>
    /// <param name="swapMode">How to swap the content (defaults to OuterHTML).</param>
    /// <returns>The builder for chaining.</returns>
    /// <example>
    /// <code>
    /// // In event chain configuration:
    /// events.When(OrderEvents.StatusChanged)
    ///       .RefreshPartial("order-status", "_OrderStatus", (ctx, payload) => {
    ///           var order = (Order)payload!; // Reuse the order from the event
    ///           return new OrderStatusViewModel { Order = order };
    ///       });
    /// 
    /// // In controller:
    /// await PublishAsync(OrderEvents.StatusChanged, order);
    /// </code>
    /// </example>
    public HttpEventChainBuilder RefreshPartial(
        string targetId,
        string viewName,
        Func<HttpContext, object?, object?> modelFactory,
        SwapMode swapMode = SwapMode.OuterHTML)
    {
        _config.Partials.Add(new EventPartialHandler(targetId, viewName, null, modelFactory, null, null, swapMode));
        return this;
    }

    /// <summary>
    /// Adds an asynchronous partial view refresh to the event chain.
    /// When the event is triggered, this partial will be rendered and sent as an OOB swap.
    /// </summary>
    /// <param name="targetId">The ID of the element to update.</param>
    /// <param name="viewName">The partial view to render.</param>
    /// <param name="modelFactory">Async factory function to create the model from HttpContext.</param>
    /// <param name="swapMode">How to swap the content (defaults to OuterHTML).</param>
    /// <returns>The builder for chaining.</returns>
    public HttpEventChainBuilder RefreshPartialAsync(
        string targetId,
        string viewName,
        Func<HttpContext, Task<object?>> modelFactory,
        SwapMode swapMode = SwapMode.OuterHTML)
    {
        _config.Partials.Add(new EventPartialHandler(targetId, viewName, null, null, modelFactory, null, swapMode));
        return this;
    }

    /// <summary>
    /// Adds an asynchronous partial view refresh with access to the event payload.
    /// When the event is triggered, the model factory receives both HttpContext and the event payload.
    /// </summary>
    /// <param name="targetId">The ID of the element to update.</param>
    /// <param name="viewName">The partial view to render.</param>
    /// <param name="modelFactory">Async factory function receiving HttpContext and event payload.</param>
    /// <param name="swapMode">How to swap the content (defaults to OuterHTML).</param>
    /// <returns>The builder for chaining.</returns>
    public HttpEventChainBuilder RefreshPartialAsync(
        string targetId,
        string viewName,
        Func<HttpContext, object?, Task<object?>> modelFactory,
        SwapMode swapMode = SwapMode.OuterHTML)
    {
        _config.Partials.Add(new EventPartialHandler(targetId, viewName, null, null, null, modelFactory, swapMode));
        return this;
    }

    /// <summary>
    /// Adds a toast notification to the event chain.
    /// When the event is triggered, this toast will be shown.
    /// </summary>
    /// <param name="message">The toast message.</param>
    /// <param name="type">The toast type (defaults to Info).</param>
    /// <returns>The builder for chaining.</returns>
    public HttpEventChainBuilder Toast(string message, ToastType type = ToastType.Info)
    {
        _config.Toasts.Add(new EventToastHandler(message, type));
        return this;
    }

    /// <summary>
    /// Adds a success toast notification to the event chain.
    /// </summary>
    public HttpEventChainBuilder SuccessToast(string message)
        => Toast(message, ToastType.Success);

    /// <summary>
    /// Adds an error toast notification to the event chain.
    /// </summary>
    public HttpEventChainBuilder ErrorToast(string message)
        => Toast(message, ToastType.Error);

    /// <summary>
    /// Adds a warning toast notification to the event chain.
    /// </summary>
    public HttpEventChainBuilder WarningToast(string message)
        => Toast(message, ToastType.Warning);

    /// <summary>
    /// Adds an info toast notification to the event chain.
    /// </summary>
    public HttpEventChainBuilder InfoToast(string message)
        => Toast(message, ToastType.Info);

    /// <summary>
    /// Adds a redirect to the event chain.
    /// When the event is triggered, the browser will navigate to the specified URL.
    /// </summary>
    /// <param name="url">The URL to redirect to.</param>
    /// <returns>The builder for chaining.</returns>
    public HttpEventChainBuilder Redirect(string url)
    {
        _config.Redirect = new EventRedirectHandler(url);
        return this;
    }

    /// <summary>
    /// Adds an additional trigger event to the chain.
    /// When the source event is triggered, this additional event will also be triggered.
    /// </summary>
    /// <param name="eventKey">The event to trigger.</param>
    /// <returns>The builder for chaining.</returns>
    public HttpEventChainBuilder AlsoTrigger(EventKey eventKey)
    {
        _config.TriggerEvents.Add(eventKey);
        // Also add to the standard event chain for backwards compatibility
        _options.Chain(_eventKey, eventKey);
        return this;
    }

    /// <summary>
    /// Completes the chain configuration.
    /// </summary>
    /// <returns>The original options for further configuration.</returns>
    public SwapEventBusOptions Build()
    {
        return _options;
    }
}
