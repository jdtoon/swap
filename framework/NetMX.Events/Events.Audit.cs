namespace NetMX.Events;

/// <summary>
/// Audit module events (partial extension of global Events class).
/// </summary>
public static partial class Events
{
    /// <summary>
    /// AuditLog-related events from Audit module.
    /// </summary>
    public static class AuditLog
    {
        /// <summary>Event: "auditlog.created"</summary>
        public const string Created = "auditlog.created";
        
        /// <summary>Event: "auditlog.updated"</summary>
        public const string Updated = "auditlog.updated";
        
        /// <summary>Event: "auditlog.deleted"</summary>
        public const string Deleted = "auditlog.deleted";
        
        /// <summary>Event: "auditlog.queried"</summary>
        public const string Queried = "auditlog.queried";
        
        /// <summary>Event: "auditlog.viewed"</summary>
        public const string Viewed = "auditlog.viewed";
        
        /// <summary>Event: "auditlog.exported"</summary>
        public const string Exported = "auditlog.exported";
    }
    
    /// <summary>
    /// AuditEntry-related events from Audit module.
    /// </summary>
    public static class AuditEntry
    {
        /// <summary>Event: "auditentry.created"</summary>
        public const string Created = "auditentry.created";
        
        /// <summary>Event: "auditentry.recorded"</summary>
        public const string Recorded = "auditentry.recorded";
        
        /// <summary>Event: "auditentry.updated"</summary>
        public const string Updated = "auditentry.updated";
        
        /// <summary>Event: "auditentry.deleted"</summary>
        public const string Deleted = "auditentry.deleted";
        
        /// <summary>Event: "auditentry.viewed"</summary>
        public const string Viewed = "auditentry.viewed";
    }
    
    /// <summary>
    /// EntityChange-related events from Audit module.
    /// </summary>
    public static class EntityChange
    {
        /// <summary>Event: "entitychange.created"</summary>
        public const string Created = "entitychange.created";
        
        /// <summary>Event: "entitychange.updated"</summary>
        public const string Updated = "entitychange.updated";
        
        /// <summary>Event: "entitychange.deleted"</summary>
        public const string Deleted = "entitychange.deleted";
        
        /// <summary>Event: "entitychange.tracked"</summary>
        public const string Tracked = "entitychange.tracked";
        
        /// <summary>Event: "entitychange.property.changed"</summary>
        public const string PropertyChanged = "entitychange.property.changed";
    }
    
    /// <summary>
    /// Compliance-related events from Audit module.
    /// </summary>
    public static class Compliance
    {
        /// <summary>Event: "compliance.retention.applied"</summary>
        public const string RetentionApplied = "compliance.retention.applied";
        
        /// <summary>Event: "compliance.report.generated"</summary>
        public const string ReportGenerated = "compliance.report.generated";
        
        /// <summary>Event: "compliance.suspicious.activity"</summary>
        public const string SuspiciousActivity = "compliance.suspicious.activity";
        
        /// <summary>Event: "compliance.violation.detected"</summary>
        public const string ViolationDetected = "compliance.violation.detected";
        
        /// <summary>Event: "compliance.policy.updated"</summary>
        public const string PolicyUpdated = "compliance.policy.updated";
    }
}
