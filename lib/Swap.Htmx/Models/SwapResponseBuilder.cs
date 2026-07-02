using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using Swap.Htmx.Results;
using Swap.Htmx.State;
using System.Text.RegularExpressions;

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
    None,

    /// <summary>
    /// Morph the entire target element (attributes + children) via the idiomorph extension,
    /// preserving focus, caret, scroll position and in-flight transitions. Requires the client
    /// idiomorph extension (auto-included by the <c>&lt;swap-scripts&gt;</c> tag helper).
    /// </summary>
    MorphOuter,

    /// <summary>
    /// Morph only the target's inner content via the idiomorph extension, preserving focus, caret
    /// and scroll. Requires the client idiomorph extension.
    /// </summary>
    MorphInner
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
    bool ConditionalExists = false,
    long? Seq = null
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
/// Represents SPA-style navigation options for HX-Location.
/// </summary>
public sealed record NavigationOptions(
    string Path,
    string Target = "#main-content",
    bool PushUrl = true
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
    private readonly List<string> _invalidatedTopics = new();
    private readonly List<ToastNotification> _toasts = new();
    private readonly List<ToastNotification> _flashToasts = new();
    private readonly List<TriggerEvent> _triggers = new();
    private readonly List<ClientAction> _clientActions = new();
    private string? _redirectUrl;
    private NavigationOptions? _navigation;
    private HxLocationOptions? _navigationOptions;
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

    private static readonly Regex ValidIdRegex = new(@"^[a-zA-Z][a-zA-Z0-9_-]*$", RegexOptions.Compiled);

    private static string NormalizeOobTargetId(string targetId)
    {
        // OOB swaps target element IDs. Many callers naturally pass CSS selectors like "#sidebar".
        // Normalizing here prevents invalid HTML like id="#sidebar" and avoids silent swap failures.
        if (string.IsNullOrWhiteSpace(targetId))
            throw new ArgumentException("OOB target ID cannot be null or empty.", nameof(targetId));

        var normalized = targetId.Trim();
        if (normalized.StartsWith('#'))
        {
            normalized = normalized.TrimStart('#').Trim();
        }

        if (!ValidIdRegex.IsMatch(normalized))
            throw new ArgumentException(
                $"OOB target ID '{normalized}' contains invalid characters. " +
                "IDs must start with a letter and contain only letters, digits, hyphens, and underscores.",
                nameof(targetId));

        return normalized;
    }

    /// <summary>
    /// Adds an out-of-band swap to update another part of the page.
    /// </summary>
    /// <param name="targetId">The ID of the element to update.</param>
    /// <param name="viewName">The partial view to render for this target.</param>
    /// <param name="model">The model for the partial view.</param>
    /// <param name="swapMode">How to swap the content (defaults to OuterHTML).</param>
    /// <param name="seq">Optional monotonic version stamp (e.g. a rowversion). The client drops any OOB swap whose <c>data-swap-seq</c> is not newer than the last applied, guarding against out-of-order or duplicate updates.</param>
    /// <returns>The builder for chaining.</returns>
    public SwapResponseBuilder AlsoUpdate(
        string targetId,
        string viewName,
        object? model = null,
        SwapMode swapMode = SwapMode.OuterHTML,
        long? seq = null)
    {
        _oobSwaps.Add(new OobSwap(NormalizeOobTargetId(targetId), viewName, model, swapMode, Seq: seq));
        return this;
    }

    /// <summary>
    /// Adds an out-of-band swap that morphs the target via idiomorph — preserving focus, caret,
    /// scroll position and in-flight CSS transitions instead of destructively replacing it.
    /// </summary>
    /// <param name="targetId">The ID of the element to morph.</param>
    /// <param name="viewName">The partial view to render for this target.</param>
    /// <param name="model">The model for the partial view.</param>
    /// <param name="innerHtml">When true, morph only the target's inner content; otherwise morph the whole element.</param>
    /// <param name="seq">Optional monotonic version stamp; see <see cref="AlsoUpdate"/>.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>Requires the client idiomorph extension, auto-included by the <c>&lt;swap-scripts&gt;</c> tag helper.</remarks>
    public SwapResponseBuilder AlsoMorph(
        string targetId,
        string viewName,
        object? model = null,
        bool innerHtml = false,
        long? seq = null)
    {
        return AlsoUpdate(targetId, viewName, model, innerHtml ? SwapMode.MorphInner : SwapMode.MorphOuter, seq);
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
        _oobSwaps.Add(new OobSwap(NormalizeOobTargetId(targetId), viewName, model, swapMode, ConditionalExists: true));
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
            _oobSwaps.Add(new OobSwap(NormalizeOobTargetId(targetId), viewName, model, swapMode));
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
    /// Adds a "flash" toast that survives an htmx-driven navigation: it is stashed in TempData and
    /// re-emitted as an <c>HX-Trigger</c> <c>showToast</c> on the next response, so a
    /// mutate → navigate → confirm flow shows its message after the navigation lands.
    /// </summary>
    /// <remarks>
    /// This fires only when the next request is htmx-issued — a boosted link/form, or
    /// <see cref="WithNavigation(string, string, bool)"/>'s HX-Location — because htmx only processes
    /// <c>HX-Trigger</c> response headers on requests it makes. A full-page <see cref="WithRedirect"/>
    /// reload does not surface it. MVC and Razor Pages have TempData; the Minimal-API result has none,
    /// so a flash there emits immediately on the current response.
    /// </remarks>
    /// <param name="message">The toast message.</param>
    /// <param name="type">The toast type (defaults to Info).</param>
    /// <returns>The builder for chaining.</returns>
    public SwapResponseBuilder WithFlash(string message, ToastType type = ToastType.Info)
    {
        _flashToasts.Add(new ToastNotification(message, type));
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
    /// This triggers a full page reload to the specified URL.
    /// </summary>
    /// <param name="url">The URL to redirect to.</param>
    /// <returns>The builder for chaining.</returns>
    public SwapResponseBuilder WithRedirect(string url)
    {
        ValidateUrl(url, nameof(url));
        _redirectUrl = url;
        return this;
    }

    /// <summary>
    /// Navigates to a URL using HTMX's HX-Location header (SPA-style, no full page reload).
    /// Content is fetched and swapped into the target element, and the browser URL is updated.
    /// </summary>
    /// <param name="path">The URL path to navigate to.</param>
    /// <param name="target">The CSS selector for the target element (defaults to "#main-content").</param>
    /// <param name="pushUrl">Whether to push the URL to browser history (defaults to true).</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// Usage:
    /// <code>
    /// // SPA-style navigation - content swapped, URL updated, no reload
    /// return SwapResponse()
    ///     .WithNavigation("/debtors/statements")
    ///     .WithSuccessToast("Statement generated")
    ///     .Build();
    /// 
    /// // Navigate to specific target
    /// return SwapResponse()
    ///     .WithNavigation("/inbox", target: "#sidebar")
    ///     .Build();
    /// </code>
    /// </remarks>
    public SwapResponseBuilder WithNavigation(string path, string target = "#main-content", bool pushUrl = true)
    {
        ValidateUrl(path, nameof(path));
        _navigation = new NavigationOptions(path, target, pushUrl);
        return this;
    }

    /// <summary>
    /// Validates that a URL is safe for redirect/navigation using an allowlist: only http/https
    /// absolute URLs and same-origin relative references are permitted. Any other scheme
    /// (<c>javascript:</c>, <c>data:</c>, <c>vbscript:</c>, <c>file:</c>, <c>mailto:</c>, …) and
    /// protocol-relative URLs (<c>//host</c>, <c>/\host</c>) are rejected as open-redirect / XSS vectors.
    /// </summary>
    private static void ValidateUrl(string? url, string paramName)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty.", paramName);

        var trimmed = url.Trim();

        // Absolute URL with an explicit scheme: allow only http/https (allowlist, not blocklist).
        if (HasScheme(trimmed, out var scheme))
        {
            if (!scheme.Equals("http", StringComparison.OrdinalIgnoreCase) &&
                !scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"URL '{trimmed}' uses the disallowed scheme '{scheme}:'. " +
                    "Only http/https absolute URLs or same-origin relative paths are permitted.",
                    paramName);
            }

            return;
        }

        // No scheme. Reject protocol-relative / backslash-obfuscated URLs (//host, /\host, \\host, \/host);
        // the browser resolves these to an off-site absolute URL, enabling open redirects.
        if (trimmed.Length >= 2)
        {
            char c0 = trimmed[0], c1 = trimmed[1];
            if ((c0 == '/' || c0 == '\\') && (c1 == '/' || c1 == '\\'))
            {
                throw new ArgumentException(
                    $"URL '{trimmed}' is a protocol-relative URL that can redirect off-site. " +
                    "Use a rooted path ('/path') or an absolute http/https URL.",
                    paramName);
            }
        }

        // Otherwise it's a same-origin relative reference ('/path', 'path', '?query', '#fragment') — allowed.
    }

    /// <summary>
    /// Returns true if <paramref name="url"/> begins with an RFC 3986 URI scheme (e.g. <c>https:</c>,
    /// <c>javascript:</c>) that appears before any '/', '?' or '#'. Outputs the scheme name (without the colon).
    /// </summary>
    private static bool HasScheme(string url, out string scheme)
    {
        scheme = string.Empty;

        if (url.Length == 0 || !char.IsLetter(url[0]))
            return false;

        for (var i = 0; i < url.Length; i++)
        {
            var c = url[i];
            if (c == ':')
            {
                scheme = url.Substring(0, i);
                return i > 0;
            }

            // A path/query/fragment delimiter before ':' means there is no scheme (it's relative).
            if (c == '/' || c == '?' || c == '#')
                return false;

            if (!(char.IsLetterOrDigit(c) || c == '+' || c == '-' || c == '.'))
                return false;
        }

        return false;
    }

    /// <summary>
    /// Navigates using HTMX's HX-Location with full control over options.
    /// </summary>
    /// <param name="options">The HX-Location options.</param>
    /// <returns>The builder for chaining.</returns>
    public SwapResponseBuilder WithNavigation(HxLocationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateUrl(options.Path, nameof(options));
        _navigationOptions = options;
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
    /// Marks one or more data topics as changed. The engine re-renders every registered fragment that
    /// <c>DependsOn</c> any of these topics — once each, deduplicated — as OOB swaps, so you invalidate
    /// what changed instead of naming every dependent widget.
    /// </summary>
    /// <param name="topics">The topics that changed (as registered via <c>o.Fragments.Fragment(...).DependsOn(...)</c>).</param>
    /// <returns>The builder for chaining.</returns>
    public SwapResponseBuilder Invalidate(params string[] topics)
    {
        if (topics != null)
        {
            foreach (var topic in topics)
            {
                if (!string.IsNullOrWhiteSpace(topic))
                {
                    _invalidatedTopics.Add(topic.Trim());
                }
            }
        }

        return this;
    }

    /// <summary>Topics invalidated on this response (drives dependency-graph fragment re-rendering).</summary>
    internal IReadOnlyList<string> InvalidatedTopics => _invalidatedTopics;

    /// <summary>
    /// Gets all configured toasts.
    /// </summary>
    public IReadOnlyList<ToastNotification> Toasts => _toasts;

    /// <summary>Flash toasts to stash in TempData for re-emission on the next response.</summary>
    internal IReadOnlyList<ToastNotification> FlashToasts => _flashToasts;

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
    /// Gets the configured SPA navigation options.
    /// </summary>
    internal NavigationOptions? Navigation => _navigation;

    /// <summary>
    /// Gets the configured HX-Location options for advanced navigation.
    /// </summary>
    internal HxLocationOptions? NavigationOptions => _navigationOptions;
    
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
