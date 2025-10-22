namespace NetMX.Events;

/// <summary>
/// Global access point for registered events.
/// </summary>
/// <remarks>
/// <para>
/// This static class provides convenient access to event names without requiring
/// project references between modules. It must be initialized at application startup
/// after all modules have registered their events.
/// </para>
/// <para>
/// <strong>Usage Pattern:</strong>
/// <code>
/// // Initialize at startup (after all modules registered events)
/// var registry = app.Services.GetRequiredService&lt;IEventRegistry&gt;();
/// Events.Initialize(registry);
/// 
/// // Use anywhere in application - Type-safe IntelliSense support!
/// await _eventBus.PublishAsync(Events.Permission.Created, data);
/// await _eventBus.PublishAsync(Events.User.Login.Success, data);
/// 
/// // Or runtime lookup
/// await _eventBus.PublishAsync(Events.Get("permission.created"), data);
/// 
/// // Check if event exists
/// if (Events.Exists("permission.created"))
/// {
///     // Event is registered
/// }
/// </code>
/// </para>
/// <para>
/// <strong>Architecture Notes:</strong>
/// <list type="bullet">
/// <item><description><strong>Monolith:</strong> Single registry, all events in one process</description></item>
/// <item><description><strong>Modular Monolith:</strong> Modules register independently, single registry</description></item>
/// <item><description><strong>Microservices:</strong> Each service has own registry, shared event contracts</description></item>
/// </list>
/// </para>
/// </remarks>
public static partial class Events
{
    private static IEventRegistry? _registry;
    private static readonly object _lock = new();
    
    /// <summary>
    /// Initializes the global event access with the provided registry.
    /// </summary>
    /// <param name="registry">The event registry to use for lookups.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="registry"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if already initialized.</exception>
    /// <remarks>
    /// This method should be called once during application startup, after all modules
    /// have registered their events and ValidateUniqueness() has been called.
    /// </remarks>
    public static void Initialize(IEventRegistry registry)
    {
        if (registry == null)
        {
            throw new ArgumentNullException(nameof(registry));
        }
        
        lock (_lock)
        {
            if (_registry != null)
            {
                throw new InvalidOperationException(
                    "Events class has already been initialized. " +
                    "Initialize should only be called once during application startup.");
            }
            
            _registry = registry;
        }
    }
    
    /// <summary>
    /// Gets the event name for the specified event.
    /// </summary>
    /// <param name="name">The event name to retrieve.</param>
    /// <returns>The event name (same as input, but validated).</returns>
    /// <exception cref="InvalidOperationException">Thrown if not initialized.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if event not registered.</exception>
    /// <remarks>
    /// This method validates that the event exists in the registry.
    /// Use this when you need runtime validation of event names.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Get event name (validates it exists)
    /// string eventName = Events.Get("permission.created");
    /// await _eventBus.PublishAsync(eventName, new { permissionId = 123 });
    /// </code>
    /// </example>
    public static string Get(string name)
    {
        EnsureInitialized();
        
        // GetEvent will throw KeyNotFoundException if not found
        var metadata = _registry!.GetEvent(name);
        return metadata.Name;
    }
    
    /// <summary>
    /// Checks if the specified event is registered.
    /// </summary>
    /// <param name="name">The event name to check.</param>
    /// <returns>True if the event is registered, false otherwise.</returns>
    /// <exception cref="InvalidOperationException">Thrown if not initialized.</exception>
    /// <remarks>
    /// Use this method to check if an event exists before attempting to publish it,
    /// especially when event names are constructed dynamically.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (Events.Exists("permission.created"))
    /// {
    ///     await _eventBus.PublishAsync("permission.created", data);
    /// }
    /// </code>
    /// </example>
    public static bool Exists(string name)
    {
        EnsureInitialized();
        return _registry!.IsRegistered(name);
    }
    
    /// <summary>
    /// Gets all registered events.
    /// </summary>
    /// <returns>A read-only dictionary of all registered events.</returns>
    /// <exception cref="InvalidOperationException">Thrown if not initialized.</exception>
    /// <remarks>
    /// Useful for debugging, introspection, or building event documentation.
    /// </remarks>
    /// <example>
    /// <code>
    /// // List all events
    /// var allEvents = Events.GetAll();
    /// foreach (var (name, metadata) in allEvents)
    /// {
    ///     Console.WriteLine($"{name} - {metadata.Description}");
    /// }
    /// </code>
    /// </example>
    public static IReadOnlyDictionary<string, EventMetadata> GetAll()
    {
        EnsureInitialized();
        return _registry!.GetAllEvents();
    }
    
    /// <summary>
    /// Gets metadata for the specified event.
    /// </summary>
    /// <param name="name">The event name to get metadata for.</param>
    /// <returns>The event metadata.</returns>
    /// <exception cref="InvalidOperationException">Thrown if not initialized.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if event not registered.</exception>
    /// <remarks>
    /// Use this to access event metadata such as module, category, direction, etc.
    /// </remarks>
    /// <example>
    /// <code>
    /// var metadata = Events.GetMetadata("permission.created");
    /// Console.WriteLine($"Module: {metadata.Module}");
    /// Console.WriteLine($"Category: {metadata.Category}");
    /// Console.WriteLine($"Direction: {metadata.Direction}");
    /// </code>
    /// </example>
    public static EventMetadata GetMetadata(string name)
    {
        EnsureInitialized();
        return _registry!.GetEvent(name);
    }
    
    /// <summary>
    /// Resets the initialization state (for testing only).
    /// </summary>
    /// <remarks>
    /// <strong>WARNING:</strong> This method is intended for testing purposes only.
    /// Do not call this in production code.
    /// </remarks>
    public static void Reset()
    {
        lock (_lock)
        {
            _registry = null;
        }
    }
    
    private static void EnsureInitialized()
    {
        if (_registry == null)
        {
            throw new InvalidOperationException(
                "Events class has not been initialized. " +
                "Call Events.Initialize(registry) during application startup.");
        }
    }
    
    // Future: Type-safe nested classes generated by source generator
    // public static class Permission
    // {
    //     public const string Created = "permission.created";
    //     public const string Updated = "permission.updated";
    //     public const string Deleted = "permission.deleted";
    // }
}
