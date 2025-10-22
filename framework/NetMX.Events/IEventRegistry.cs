namespace NetMX.Events;

/// <summary>
/// Central registry for all events in the application.
/// Modules register their events at startup for global discovery and validation.
/// </summary>
/// <remarks>
/// <para>
/// The Event Registry enables loose coupling between modules while maintaining
/// type safety and discoverability. Modules define their own events and register
/// them at startup. Other modules can then access these events without direct
/// project references.
/// </para>
/// <para>
/// <strong>Architecture Support:</strong>
/// <list type="bullet">
/// <item><description><strong>Basic Monolith</strong>: Single registry, in-process events</description></item>
/// <item><description><strong>Modular Monolith</strong>: Single registry, modules register independently</description></item>
/// <item><description><strong>Microservices</strong>: Per-service registry + shared event contracts</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register event at module startup
/// var registry = app.Services.GetRequiredService&lt;IEventRegistry&gt;();
/// registry.RegisterEvent("permission.created", new EventMetadata
/// {
///     Name = "permission.created",
///     Module = "Authorization",
///     Category = "Permission",
///     Direction = EventDirection.Upstream
/// });
/// 
/// // Access event globally
/// var eventName = Events.Get("permission.created");
/// this.HxTrigger(eventName, new { id = permissionId });
/// </code>
/// </example>
public interface IEventRegistry
{
    /// <summary>
    /// Register an event in the registry.
    /// </summary>
    /// <param name="name">The event name (e.g., "permission.created").</param>
    /// <param name="metadata">Metadata describing the event.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if an event with the same name is already registered.
    /// </exception>
    /// <remarks>
    /// Event names must be unique across all modules. Use the format:
    /// <c>{entity}.{action}</c> (e.g., "permission.created", "user.updated").
    /// </remarks>
    void RegisterEvent(string name, EventMetadata metadata);
    
    /// <summary>
    /// Get event metadata by name.
    /// </summary>
    /// <param name="name">The event name to look up.</param>
    /// <returns>The event metadata.</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the event is not registered.
    /// </exception>
    EventMetadata GetEvent(string name);
    
    /// <summary>
    /// Get all registered events.
    /// </summary>
    /// <returns>
    /// A read-only dictionary of event names to metadata.
    /// </returns>
    IReadOnlyDictionary<string, EventMetadata> GetAllEvents();
    
    /// <summary>
    /// Check if an event is registered.
    /// </summary>
    /// <param name="name">The event name to check.</param>
    /// <returns>
    /// <c>true</c> if the event is registered; otherwise, <c>false</c>.
    /// </returns>
    bool IsRegistered(string name);
    
    /// <summary>
    /// Validate that all events have unique names.
    /// Call this after all modules have registered their events.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if duplicate event names are detected.
    /// </exception>
    /// <remarks>
    /// This method is idempotent and can be called multiple times.
    /// It's recommended to call this in <c>Program.cs</c> after all
    /// modules have initialized.
    /// </remarks>
    void ValidateUniqueness();
}
