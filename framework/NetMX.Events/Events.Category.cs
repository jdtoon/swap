namespace NetMX.Events;

/// <summary>
/// Category entity events (partial extension of global Events class).
/// </summary>
/// <remarks>
/// This partial class extends the global <see cref="Events"/> class with
/// Category-specific event constants. This allows type-safe access like:
/// <code>Events.Category.Created</code> from any module without project references.
/// </remarks>
public static partial class Events
{
    /// <summary>
    /// Category-related events.
    /// </summary>
    public static class Category
    {
        /// <summary>
        /// Event: "category.created"
        /// </summary>
        /// <remarks>
        /// Triggered when a new Category is created.
        /// Payload: { categoryId: Guid }
        /// </remarks>
        public const string Created = "category.created";
        
        /// <summary>
        /// Event: "category.updated"
        /// </summary>
        /// <remarks>
        /// Triggered when a Category is updated.
        /// Payload: { categoryId: Guid, changes: string[] }
        /// </remarks>
        public const string Updated = "category.updated";
        
        /// <summary>
        /// Event: "category.deleted"
        /// </summary>
        /// <remarks>
        /// Triggered when a Category is deleted.
        /// Payload: { categoryId: Guid }
        /// </remarks>
        public const string Deleted = "category.deleted";
    }
}
