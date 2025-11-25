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
    /// Internal list of configuration types to be instantiated and applied.
    /// </summary>
    internal List<Type> ConfigurationTypes { get; } = new();

    /// <summary>
    /// Registers a feature-specific event configuration.
    /// </summary>
    /// <typeparam name="T">The configuration type implementing ISwapEventConfiguration.</typeparam>
    public void AddConfig<T>() where T : ISwapEventConfiguration, new()
    {
        ConfigurationTypes.Add(typeof(T));
    }
}
