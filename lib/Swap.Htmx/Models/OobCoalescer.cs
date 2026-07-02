namespace Swap.Htmx.Models;

/// <summary>
/// Collapses duplicate out-of-band swaps targeting the same element ID so the client never applies
/// a redundant double-swap. Replace-style swaps (which discard the target's prior content wholesale)
/// only need the last one queued for a given target; insert-style swaps accumulate and are all kept.
/// </summary>
internal static class OobCoalescer
{
    /// <summary>
    /// Returns true if <paramref name="mode"/> discards the target's prior content instead of
    /// appending alongside it. Insert-style positions (<see cref="SwapMode.BeforeBegin"/>,
    /// <see cref="SwapMode.AfterBegin"/>, <see cref="SwapMode.BeforeEnd"/>, <see cref="SwapMode.AfterEnd"/>)
    /// are the only non-replace modes; everything else — including <see cref="SwapMode.None"/>, which
    /// still designates a single logical update to the target — is treated as replace-style.
    /// </summary>
    private static bool IsReplaceStyle(SwapMode mode) => mode switch
    {
        SwapMode.BeforeBegin => false,
        SwapMode.AfterBegin => false,
        SwapMode.BeforeEnd => false,
        SwapMode.AfterEnd => false,
        _ => true
    };

    /// <summary>
    /// Coalesces a list of OOB swaps: for each target ID, only the last replace-style swap is kept
    /// (earlier ones are dropped as redundant), while insert-style swaps to any target all survive.
    /// The original relative order of the surviving swaps is preserved.
    /// </summary>
    /// <param name="swaps">The OOB swaps queued by the builder, in the order they were added.</param>
    /// <returns>The coalesced list, safe to render in place of <paramref name="swaps"/>.</returns>
    public static List<OobSwap> Coalesce(IReadOnlyList<OobSwap> swaps)
    {
        ArgumentNullException.ThrowIfNull(swaps);

        if (swaps.Count == 0)
            return new List<OobSwap>();

        // For each target, find the index of the last replace-style swap; earlier replace-style
        // swaps to that same target are dropped. Insert-style swaps are never dropped.
        var lastReplaceIndexByTarget = new Dictionary<string, int>();
        for (var i = 0; i < swaps.Count; i++)
        {
            var swap = swaps[i];
            if (IsReplaceStyle(swap.SwapMode))
            {
                lastReplaceIndexByTarget[swap.TargetId] = i;
            }
        }

        var result = new List<OobSwap>(swaps.Count);
        for (var i = 0; i < swaps.Count; i++)
        {
            var swap = swaps[i];
            if (!IsReplaceStyle(swap.SwapMode) ||
                lastReplaceIndexByTarget[swap.TargetId] == i)
            {
                result.Add(swap);
            }
        }

        return result;
    }
}
