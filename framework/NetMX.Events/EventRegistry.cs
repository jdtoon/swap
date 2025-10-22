using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace NetMX.Events;

/// <summary>
/// Thread-safe implementation of <see cref="IEventRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// for thread-safe event registration and lookup. Multiple modules can register
/// events concurrently during application startup.
/// </para>
/// <para>
/// <strong>Usage Pattern:</strong>
/// <code>
/// // Register at module startup
/// var registry = app.Services.GetRequiredService&lt;IEventRegistry&gt;();
/// registry.RegisterEvent("permission.created", new EventMetadata
/// {
///     Name = "permission.created",
///     Module = "Authorization",
///     Category = "Permission",
///     Direction = EventDirection.Upstream
/// });
/// 
/// // Validate after all modules loaded
/// registry.ValidateUniqueness();
/// 
/// // Initialize global access
/// Events.Initialize(registry);
/// </code>
/// </para>
/// </remarks>
public class EventRegistry : IEventRegistry
{
    private readonly ConcurrentDictionary<string, EventMetadata> _events = new();
    private bool _validated = false;
    private readonly object _validationLock = new();
    
    /// <inheritdoc />
    public void RegisterEvent(string name, EventMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Event name cannot be null or whitespace.", 
                nameof(name));
        }
        
        if (metadata == null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }
        
        if (name != metadata.Name)
        {
            throw new ArgumentException(
                $"Event name '{name}' does not match metadata name '{metadata.Name}'.",
                nameof(name));
        }
        
        // Try to add the event
        if (!_events.TryAdd(name, metadata))
        {
            // Event already exists - get existing for error message
            var existing = _events[name];
            throw new InvalidOperationException(
                $"Event '{name}' is already registered by module '{existing.Module}'. " +
                $"Cannot register duplicate event from module '{metadata.Module}'. " +
                $"Event names must be unique across all modules.");
        }
    }
    
    /// <inheritdoc />
    public EventMetadata GetEvent(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Event name cannot be null or whitespace.", 
                nameof(name));
        }
        
        if (!_events.TryGetValue(name, out var metadata))
        {
            throw new KeyNotFoundException(
                $"Event '{name}' is not registered. " +
                $"Available events: {string.Join(", ", _events.Keys.Take(10))}" +
                (_events.Count > 10 ? $" (and {_events.Count - 10} more)" : ""));
        }
        
        return metadata;
    }
    
    /// <inheritdoc />
    public IReadOnlyDictionary<string, EventMetadata> GetAllEvents()
    {
        // Return immutable snapshot
        return _events.ToImmutableDictionary();
    }
    
    /// <inheritdoc />
    public bool IsRegistered(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }
        
        return _events.ContainsKey(name);
    }
    
    /// <inheritdoc />
    public void ValidateUniqueness()
    {
        lock (_validationLock)
        {
            // Check if already validated
            if (_validated)
            {
                return; // Already validated, no need to check again
            }
            
            // Group by event name to find duplicates
            // (In practice, ConcurrentDictionary prevents duplicates,
            // but this provides a clear validation checkpoint)
            var duplicates = _events
                .GroupBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();
            
            if (duplicates.Any())
            {
                var errors = string.Join("\n", duplicates.Select(d =>
                    $"  - '{d.Key}' registered by: {string.Join(", ", d.Select(e => e.Value.Module))}"));
                
                throw new InvalidOperationException(
                    $"Duplicate event names detected (case-insensitive):\n{errors}\n" +
                    "Event names must be unique across all modules. " +
                    "Consider using prefixes (e.g., 'auth.permission.created' vs 'catalog.permission.created').");
            }
            
            // Additional validation: Check for naming convention violations
            var violations = _events
                .Where(e => !IsValidEventName(e.Key))
                .ToList();
            
            if (violations.Any())
            {
                var errors = string.Join("\n", violations.Select(v =>
                    $"  - '{v.Key}' from module '{v.Value.Module}'"));
                
                throw new InvalidOperationException(
                    $"Event naming convention violations detected:\n{errors}\n" +
                    "Event names should follow the format: {entity}.{action} (lowercase with dots). " +
                    "Examples: 'permission.created', 'user.updated', 'login.success'");
            }
            
            _validated = true;
        }
    }
    
    /// <summary>
    /// Validates event name follows naming convention.
    /// </summary>
    /// <param name="name">The event name to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    /// <remarks>
    /// Valid format: lowercase letters, numbers, dots, and hyphens only.
    /// Must contain at least one dot.
    /// Examples: "permission.created", "user.password-changed", "order-item.deleted"
    /// </remarks>
    private static bool IsValidEventName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }
        
        // Must contain at least one dot (entity.action format)
        if (!name.Contains('.'))
        {
            return false;
        }
        
        // Must be lowercase (or numbers, dots, hyphens)
        if (name != name.ToLowerInvariant())
        {
            return false;
        }
        
        // Only allow: lowercase letters, numbers, dots, hyphens
        return name.All(c => char.IsLower(c) || char.IsDigit(c) || c == '.' || c == '-');
    }
}
