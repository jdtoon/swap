namespace NetMX.Events;

/// <summary>
/// Event constants for Category entity.
/// </summary>
/// <remarks>
/// This partial class extends DomainEvents with Category-specific events.
/// Use these constants in controllers and views for type-safe event communication.
/// </remarks>
public static partial class DomainEvents
{
    /// <summary>
    /// Events for Category entity
    /// </summary>
    public static class Category
    {
        /// <summary>
        /// Triggered when a new Category is created.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Created = "category-created";

        /// <summary>
        /// Triggered when an existing Category is updated.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Updated = "category-updated";

        /// <summary>
        /// Triggered when a Category is deleted.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Deleted = "category-deleted";
    }
}