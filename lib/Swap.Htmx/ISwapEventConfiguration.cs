using Swap.Htmx.Events;
using Swap.Htmx.Models;

namespace Swap.Htmx;

/// <summary>
/// Defines a configuration source for Swap.Htmx event chains.
/// Implement this interface to organize event configuration by feature.
/// </summary>
public interface ISwapEventConfiguration
{
    /// <summary>
    /// Configures event chains and listeners.
    /// </summary>
    /// <param name="events">The event bus options builder.</param>
    void Configure(SwapEventBusOptions events);
}
