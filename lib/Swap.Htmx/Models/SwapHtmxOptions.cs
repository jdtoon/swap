namespace Swap.Htmx.Models;

/// <summary>
/// Configuration options for Swap.Htmx library features.
/// </summary>
public class SwapHtmxOptions
{
    /// <summary>
    /// Event bus configuration for event chains and SSE.
    /// </summary>
    public Events.SwapEventBusOptions EventBus { get; set; } = new();
    
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
}
