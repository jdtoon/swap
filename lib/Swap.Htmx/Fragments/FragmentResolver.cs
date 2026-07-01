using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Swap.Htmx.Models;

namespace Swap.Htmx.Fragments;

/// <summary>
/// Turns invalidated topics into the set of OOB swaps to render, shared by every result type.
/// Explicit <c>AlsoUpdate</c> targets win over graph-derived fragments for the same id (coalescing),
/// and each fragment is produced at most once.
/// </summary>
internal static class FragmentResolver
{
    public static List<OobSwap> Resolve(
        SwapFragmentRegistry? registry,
        IReadOnlyList<string> invalidatedTopics,
        IEnumerable<string> explicitTargetIds,
        HttpContext httpContext)
    {
        var result = new List<OobSwap>();
        if (registry == null || invalidatedTopics == null || invalidatedTopics.Count == 0)
        {
            return result;
        }

        var explicitSet = new HashSet<string>(explicitTargetIds ?? Array.Empty<string>(), StringComparer.Ordinal);

        foreach (var fragment in registry.ResolveForTopics(invalidatedTopics))
        {
            // An explicit OOB swap for the same target already covers this fragment — don't double-render.
            if (explicitSet.Contains(fragment.Id))
            {
                continue;
            }

            result.Add(new OobSwap(fragment.Id, fragment.ViewName, fragment.ModelFactory(httpContext), fragment.SwapMode));
        }

        return result;
    }
}
