namespace NetMX.Events;

/// <summary>
/// Setting entity events (partial extension of global Events class).
/// </summary>
/// <remarks>
/// This partial class extends the global <see cref="Events"/> class with
/// Setting-specific event constants. This allows type-safe access like:
/// <code>Events.Setting.Created</code> from any module without project references.
/// </remarks>
public static partial class Events
{
    /// <summary>
    /// Setting-related events.
    /// </summary>
    public static class Setting
    {
        /// <summary>
        /// Event: "setting.created"
        /// </summary>
        /// <remarks>
        /// Triggered when a new Setting is created.
        /// Payload: { settingId: Guid }
        /// </remarks>
        public const string Created = "setting.created";
        
        /// <summary>
        /// Event: "setting.updated"
        /// </summary>
        /// <remarks>
        /// Triggered when a Setting is updated.
        /// Payload: { settingId: Guid, changes: string[] }
        /// </remarks>
        public const string Updated = "setting.updated";
        
        /// <summary>
        /// Event: "setting.deleted"
        /// </summary>
        /// <remarks>
        /// Triggered when a Setting is deleted.
        /// Payload: { settingId: Guid }
        /// </remarks>
        public const string Deleted = "setting.deleted";
    }
}
