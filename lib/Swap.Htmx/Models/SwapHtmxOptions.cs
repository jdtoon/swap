using System.Reflection;
using Swap.Htmx;
using Swap.Htmx.Events;

/// <summary>
/// Configuration options for Swap.Htmx library features.
/// </summary>
public class SwapHtmxOptions
{
    /// <summary>
    /// Event bus configuration for event chains and SSE.
    /// </summary>
    public SwapEventBusOptions EventBus { get; set; } = new();

    /// <summary>
    /// Default CSS selector for navigation target. Used by &lt;swap-nav&gt; tag helper.
    /// Default: "#main-content"
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddSwapHtmx(options => {
    ///     options.DefaultNavigationTarget = "main"; // or "#content"
    /// });
    /// </code>
    /// </example>
    public string DefaultNavigationTarget { get; set; } = "#main-content";

    /// <summary>
    /// Automatically suppress layout for HTMX requests (non-boosted).
    /// When true, views render without layout for HX-Request headers.
    /// Eliminates the need for _ViewStart.cshtml in each module.
    /// Default: false (for backward compatibility)
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddSwapHtmx(options => {
    ///     options.AutoSuppressLayout = true;
    /// });
    /// </code>
    /// </example>
    public bool AutoSuppressLayout { get; set; } = false;
    
    /// <summary>
    /// Folders to search when rendering OOB partial views, relative to ~/Views/.
    /// Defaults to ["Shared"]. Add folders where your partial views live to avoid
    /// "view not found" errors when using cross-controller OOB swaps.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddSwapHtmx(options => {
    ///     options.PartialViewSearchPaths.Add("Components");
    ///     options.PartialViewSearchPaths.Add("Cart");
    /// });
    /// </code>
    /// </example>
    public List<string> PartialViewSearchPaths { get; set; } = new() { "Shared" };

    /// <summary>
    /// Assemblies to scan for distributed event handlers.
    /// Defaults to the entry assembly and Swap.Htmx assembly.
    /// </summary>
    public List<Assembly> AssembliesToScan { get; set; } = new();

    /// <summary>
    /// Development and diagnostics options.
    /// </summary>
    public SwapDiagnosticsOptions Diagnostics { get; set; } = new();

    /// <summary>
    /// Internal list of configuration types to be instantiated and applied.
    /// </summary>
    internal List<Type> ConfigurationTypes { get; } = new();

    /// <summary>
    /// Configuration for global error handling in HTMX requests.
    /// </summary>
    public SwapErrorOptions ErrorHandling { get; set; } = new();

    /// <summary>
    /// Registers a feature-specific event configuration.
    /// </summary>
    /// <typeparam name="T">The configuration type implementing ISwapEventConfiguration.</typeparam>
    public void AddConfig<T>() where T : ISwapEventConfiguration, new()
    {
        ConfigurationTypes.Add(typeof(T));
    }
}

/// <summary>
/// Options for handling exceptions in HTMX requests.
/// </summary>
public class SwapErrorOptions
{
    /// <summary>
    /// Automatically catch unhandled exceptions in HTMX requests and return a formatted error response.
    /// Default: false.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The partial view to render when an error occurs.
    /// Default: "_SwapErrorToast" (assumes you have a toast container).
    /// </summary>
    public string ErrorViewName { get; set; } = "_SwapErrorToast";

    /// <summary>
    /// If true, includes the exception message in the error view model.
    /// If false, uses a generic "An error occurred" message.
    /// Default: false (true in Development).
    /// </summary>
    public bool ShowExceptionDetails { get; set; } = false;
}

/// <summary>
/// Configuration options for development diagnostics and debugging.
/// </summary>
public class SwapDiagnosticsOptions
{
    /// <summary>
    /// Enable verbose client-side event logging in the browser console.
    /// When enabled, all HX-Trigger events, OOB swaps, and state changes are logged.
    /// Default: false (auto-enabled in Development environment).
    /// </summary>
    public bool EnableClientLogging { get; set; } = false;

    /// <summary>
    /// Enable the SwapDevTools panel in the browser.
    /// Provides a visual overlay showing event flow, state, and OOB targets.
    /// Default: false.
    /// </summary>
    public bool EnableDevToolsPanel { get; set; } = false;

    /// <summary>
    /// Log warnings when events are triggered but no handlers are configured.
    /// Helps identify orphaned events during development.
    /// Default: true in Development, false in Production.
    /// </summary>
    public bool WarnOnUnhandledEvents { get; set; } = false;

    /// <summary>
    /// Log warnings when OOB swap targets may not exist in the DOM.
    /// Checks against known element IDs from previous responses.
    /// Default: true in Development, false in Production.
    /// </summary>
    public bool WarnOnMissingOobTargets { get; set; } = false;

    /// <summary>
    /// Validate event chain configurations at startup.
    /// Detects cycles, invalid names, and unreachable events.
    /// Default: true.
    /// </summary>
    public bool ValidateEventChainsOnStartup { get; set; } = true;

    /// <summary>
    /// Include detailed timing information in server logs.
    /// Shows how long event chain processing takes.
    /// Default: false.
    /// </summary>
    public bool EnableTimingLogs { get; set; } = false;
}
