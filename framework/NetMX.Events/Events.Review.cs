namespace NetMX.Events;

/// <summary>
/// Review entity events (partial extension of global Events class).
/// </summary>
/// <remarks>
/// This partial class extends the global <see cref="Events"/> class with
/// Review-specific event constants. This allows type-safe access like:
/// <code>Events.Review.Created</code> from any module without project references.
/// </remarks>
public static partial class Events
{
    /// <summary>
    /// Review-related events.
    /// </summary>
    public static class Review
    {
        /// <summary>
        /// Event: "review.created"
        /// </summary>
        /// <remarks>
        /// Triggered when a new Review is created.
        /// Payload: { reviewId: Guid }
        /// </remarks>
        public const string Created = "review.created";
        
        /// <summary>
        /// Event: "review.updated"
        /// </summary>
        /// <remarks>
        /// Triggered when a Review is updated.
        /// Payload: { reviewId: Guid, changes: string[] }
        /// </remarks>
        public const string Updated = "review.updated";
        
        /// <summary>
        /// Event: "review.deleted"
        /// </summary>
        /// <remarks>
        /// Triggered when a Review is deleted.
        /// Payload: { reviewId: Guid }
        /// </remarks>
        public const string Deleted = "review.deleted";
    }
}
