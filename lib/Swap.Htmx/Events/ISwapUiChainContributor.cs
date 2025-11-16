namespace Swap.Htmx.Events;

/// <summary>
/// Allows a module to contribute UI event chains to the Swap event bus options
/// without requiring the host to wire each module explicitly.
/// </summary>
public interface ISwapUiChainContributor
{
    /// <summary>
    /// Configure UI chains by adding Chain(...) declarations to the provided options.
    /// </summary>
    /// <param name="options">The Swap event bus options to configure.</param>
    void Configure(SwapEventBusOptions options);
}
