using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Swap.Htmx.Models;

namespace Swap.Htmx.Fragments;

/// <summary>
/// A registered fragment: a named, out-of-band-updatable UI region that declares the data topics it
/// depends on. When any of those topics is invalidated, the engine re-renders this fragment once.
/// </summary>
public sealed class FragmentDefinition
{
    private readonly HashSet<string> _topics = new(StringComparer.Ordinal);

    internal FragmentDefinition(string id, string viewName, Func<HttpContext, object?> modelFactory, SwapMode swapMode)
    {
        Id = id;
        ViewName = viewName;
        ModelFactory = modelFactory;
        SwapMode = swapMode;
    }

    /// <summary>The DOM element id this fragment targets (also its unique registry key).</summary>
    public string Id { get; }

    /// <summary>The partial view rendered for this fragment.</summary>
    public string ViewName { get; }

    /// <summary>Produces the model for the fragment for the current request.</summary>
    public Func<HttpContext, object?> ModelFactory { get; }

    /// <summary>How the fragment is swapped when re-rendered.</summary>
    public SwapMode SwapMode { get; }

    /// <summary>The topics this fragment depends on.</summary>
    public IReadOnlyCollection<string> Topics => _topics;

    internal void AddTopics(IEnumerable<string> topics)
    {
        foreach (var topic in topics)
        {
            if (!string.IsNullOrWhiteSpace(topic))
            {
                _topics.Add(topic.Trim());
            }
        }
    }
}

/// <summary>
/// Fluent handle returned by <see cref="SwapFragmentRegistry.Fragment"/> for declaring dependencies.
/// </summary>
public sealed class FragmentRegistration
{
    private readonly SwapFragmentRegistry _registry;
    private readonly FragmentDefinition _definition;

    internal FragmentRegistration(SwapFragmentRegistry registry, FragmentDefinition definition)
    {
        _registry = registry;
        _definition = definition;
    }

    /// <summary>Declares the data topics this fragment depends on; invalidating any re-renders it.</summary>
    public FragmentRegistration DependsOn(params string[] topics)
    {
        _definition.AddTopics(topics);
        _registry.IndexTopics(_definition, topics);
        return this;
    }
}

/// <summary>
/// Registry of dependency-aware fragments. Controllers invalidate a topic and the engine computes the
/// set of fragments to re-render — deduplicated — instead of every action remembering to refresh each
/// dependent widget. Registered once at startup via <c>AddSwapHtmx(o =&gt; o.Fragments.Fragment(...))</c>.
/// </summary>
public sealed class SwapFragmentRegistry
{
    private readonly List<FragmentDefinition> _order = new();
    private readonly Dictionary<string, FragmentDefinition> _byId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _byTopic = new(StringComparer.Ordinal);
    private bool _frozen;

    /// <summary>Registers a fragment. Chain <see cref="FragmentRegistration.DependsOn"/> to declare its topics.</summary>
    /// <remarks>Startup-only: throws once the registry is frozen (after <c>AddSwapHtmx</c> builds options).</remarks>
    public FragmentRegistration Fragment(string id, string viewName, Func<HttpContext, object?> modelFactory, SwapMode swapMode = SwapMode.OuterHTML)
    {
        ThrowIfFrozen();
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Fragment id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(viewName))
            throw new ArgumentException("Fragment view name is required.", nameof(viewName));
        ArgumentNullException.ThrowIfNull(modelFactory);
        if (_byId.ContainsKey(id))
            throw new InvalidOperationException($"Fragment '{id}' is already registered.");

        var definition = new FragmentDefinition(id, viewName, modelFactory, swapMode);
        _byId[id] = definition;
        _order.Add(definition);
        return new FragmentRegistration(this, definition);
    }

    internal void IndexTopics(FragmentDefinition definition, IEnumerable<string> topics)
    {
        ThrowIfFrozen();
        foreach (var topic in topics)
        {
            if (string.IsNullOrWhiteSpace(topic))
                continue;

            var key = topic.Trim();
            if (!_byTopic.TryGetValue(key, out var ids))
            {
                ids = new List<string>();
                _byTopic[key] = ids;
            }

            if (!ids.Contains(definition.Id))
            {
                ids.Add(definition.Id);
            }
        }
    }

    /// <summary>
    /// Returns every fragment that depends on any of the given topics, each at most once, in
    /// registration order.
    /// </summary>
    public IReadOnlyList<FragmentDefinition> ResolveForTopics(IEnumerable<string> topics)
    {
        var invalidated = new HashSet<string>(
            topics.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()),
            StringComparer.Ordinal);

        // Walk fragments in registration order and take each at most once if it depends on any
        // invalidated topic — deduplicated fan-out.
        var result = new List<FragmentDefinition>();
        foreach (var definition in _order)
        {
            if (definition.Topics.Any(invalidated.Contains))
            {
                result.Add(definition);
            }
        }

        return result;
    }

    /// <summary>All registered fragments, in registration order.</summary>
    public IReadOnlyCollection<FragmentDefinition> All => _order;

    /// <summary>
    /// Marks the registry immutable. Called by the DI wiring once <c>AddSwapHtmx</c> options are built, so
    /// the request-time reads (<see cref="ResolveForTopics"/>, <see cref="All"/>) can never race a write.
    /// </summary>
    internal void Freeze() => _frozen = true;

    private void ThrowIfFrozen()
    {
        if (_frozen)
        {
            throw new InvalidOperationException(
                "SwapFragmentRegistry is frozen. Register fragments at startup in " +
                "AddSwapHtmx(o => o.Fragments.Fragment(...)), not at request time.");
        }
    }
}
