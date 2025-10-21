namespace NetMX.Events;

/// <summary>
/// Event constants for TestEntity entity.
/// </summary>
/// <remarks>
/// This partial class extends DomainEvents with TestEntity-specific events.
/// Use these constants in controllers and views for type-safe event communication.
/// </remarks>
public static partial class DomainEvents
{
    /// <summary>
    /// Events for TestEntity entity
    /// </summary>
    public static class TestEntity
    {
        /// <summary>
        /// Triggered when a new TestEntity is created.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Created = "testentity-created";

        /// <summary>
        /// Triggered when an existing TestEntity is updated.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Updated = "testentity-updated";

        /// <summary>
        /// Triggered when a TestEntity is deleted.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Deleted = "testentity-deleted";
    }
}