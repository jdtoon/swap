namespace NetMX.Events;

/// <summary>
/// Metadata describing an event in the registry.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Naming Convention:</strong> Event names should follow the format
/// <c>{entity}.{action}</c> using lowercase with dots. Examples:
/// <list type="bullet">
/// <item><description>permission.created</description></item>
/// <item><description>role.updated</description></item>
/// <item><description>user.deleted</description></item>
/// <item><description>login.success</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Event Direction:</strong>
/// <list type="bullet">
/// <item><description><c>Upstream</c>: Can trigger events higher in the dependency graph</description></item>
/// <item><description><c>Downstream</c>: Can trigger events lower in the graph</description></item>
/// <item><description><c>Terminal</c>: Cannot trigger any other events (end of chain)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var metadata = new EventMetadata
/// {
///     Name = "permission.created",
///     Module = "Authorization",
///     Category = "Permission",
///     Direction = EventDirection.Upstream,
///     Description = "Triggered when a permission is created. Payload: { id: Guid }",
///     PayloadType = typeof(PermissionCreatedPayload)
/// };
/// </code>
/// </example>
public record EventMetadata
{
    /// <summary>
    /// Gets the event name (e.g., "permission.created").
    /// </summary>
    /// <remarks>
    /// Must be unique across all modules. Use lowercase with dots.
    /// Format: <c>{entity}.{action}</c>
    /// </remarks>
    public required string Name { get; init; }
    
    /// <summary>
    /// Gets the module that owns this event (e.g., "Authorization", "Identity").
    /// </summary>
    /// <remarks>
    /// Used for collision detection and debugging. Should match the module name.
    /// </remarks>
    public required string Module { get; init; }
    
    /// <summary>
    /// Gets the category/entity this event belongs to (e.g., "Permission", "Role", "User").
    /// </summary>
    /// <remarks>
    /// Used for grouping events in UI and source generators.
    /// Typically matches the entity name in singular form.
    /// </remarks>
    public required string Category { get; init; }
    
    /// <summary>
    /// Gets the event direction for DAG (Directed Acyclic Graph) enforcement.
    /// </summary>
    /// <remarks>
    /// The EventBus uses this to prevent infinite event loops.
    /// See <see cref="EventDirection"/> for details.
    /// </remarks>
    public EventDirection Direction { get; init; } = EventDirection.Downstream;
    
    /// <summary>
    /// Gets an optional description of when this event is triggered and what payload it contains.
    /// </summary>
    /// <remarks>
    /// Used for documentation and developer discoverability.
    /// Example: "Triggered when a permission is created. Payload: { id: Guid }"
    /// </remarks>
    public string? Description { get; init; }
    
    /// <summary>
    /// Gets the optional type of the event payload.
    /// </summary>
    /// <remarks>
    /// Used for strongly-typed event handlers in advanced scenarios.
    /// Most events use anonymous objects and don't need this.
    /// </remarks>
    public Type? PayloadType { get; init; }
}
