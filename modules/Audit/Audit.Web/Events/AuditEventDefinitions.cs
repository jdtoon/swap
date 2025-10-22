using NetMX.Events;

namespace NetMX.Audit.Web.Events;

/// <summary>
/// Defines and registers all Audit module events.
/// </summary>
public static class AuditEventDefinitions
{
    /// <summary>
    /// Registers all Audit events with the event registry.
    /// </summary>
    /// <param name="registry">The event registry to register events with.</param>
    public static void Register(IEventRegistry registry)
    {
        // AuditLog events
        registry.RegisterEvent(NetMX.Events.Events.AuditLog.Created, new EventMetadata
        {
            Name = NetMX.Events.Events.AuditLog.Created,
            Module = "Audit",
            Category = "AuditLog",
            Direction = EventDirection.Terminal,
            Description = "Triggered when an audit log is created. Payload: { auditLogId: Guid, entityType: string, action: string }"
        });
        
        registry.RegisterEvent(NetMX.Events.Events.AuditLog.Viewed, new EventMetadata
        {
            Name = NetMX.Events.Events.AuditLog.Viewed,
            Module = "Audit",
            Category = "AuditLog",
            Direction = EventDirection.Terminal,
            Description = "Triggered when an audit log is viewed. Payload: { auditLogId: Guid, viewedBy: Guid }"
        });
        
        registry.RegisterEvent(NetMX.Events.Events.AuditLog.Exported, new EventMetadata
        {
            Name = NetMX.Events.Events.AuditLog.Exported,
            Module = "Audit",
            Category = "AuditLog",
            Direction = EventDirection.Terminal,
            Description = "Triggered when audit logs are exported. Payload: { exportedBy: Guid, format: string, count: int }"
        });
        
        // AuditEntry events
        registry.RegisterEvent(NetMX.Events.Events.AuditEntry.Recorded, new EventMetadata
        {
            Name = NetMX.Events.Events.AuditEntry.Recorded,
            Module = "Audit",
            Category = "AuditEntry",
            Direction = EventDirection.Terminal,
            Description = "Triggered when an audit entry is recorded. Payload: { entryId: Guid, userId: Guid, action: string }"
        });
        
        registry.RegisterEvent(NetMX.Events.Events.AuditEntry.Updated, new EventMetadata
        {
            Name = NetMX.Events.Events.AuditEntry.Updated,
            Module = "Audit",
            Category = "AuditEntry",
            Direction = EventDirection.Terminal,
            Description = "Triggered when an audit entry is updated. Payload: { entryId: Guid, changes: string[] }"
        });
        
        // EntityChange events
        registry.RegisterEvent(NetMX.Events.Events.EntityChange.Tracked, new EventMetadata
        {
            Name = NetMX.Events.Events.EntityChange.Tracked,
            Module = "Audit",
            Category = "EntityChange",
            Direction = EventDirection.Terminal,
            Description = "Triggered when an entity change is tracked. Payload: { entityType: string, entityId: string, changeType: string }"
        });
        
        registry.RegisterEvent(NetMX.Events.Events.EntityChange.PropertyChanged, new EventMetadata
        {
            Name = NetMX.Events.Events.EntityChange.PropertyChanged,
            Module = "Audit",
            Category = "EntityChange",
            Direction = EventDirection.Terminal,
            Description = "Triggered when an entity property changes. Payload: { entityType: string, propertyName: string, oldValue: string, newValue: string }"
        });
        
        // Compliance events
        registry.RegisterEvent(NetMX.Events.Events.Compliance.ReportGenerated, new EventMetadata
        {
            Name = NetMX.Events.Events.Compliance.ReportGenerated,
            Module = "Audit",
            Category = "Compliance",
            Direction = EventDirection.Terminal,
            Description = "Triggered when a compliance report is generated. Payload: { reportId: Guid, generatedBy: Guid, startDate: DateTime, endDate: DateTime }"
        });
        
        registry.RegisterEvent(NetMX.Events.Events.Compliance.ViolationDetected, new EventMetadata
        {
            Name = NetMX.Events.Events.Compliance.ViolationDetected,
            Module = "Audit",
            Category = "Compliance",
            Direction = EventDirection.Upstream,
            Description = "Triggered when a compliance violation is detected. Payload: { violationId: Guid, rule: string, severity: string }"
        });
        
        registry.RegisterEvent(NetMX.Events.Events.Compliance.PolicyUpdated, new EventMetadata
        {
            Name = NetMX.Events.Events.Compliance.PolicyUpdated,
            Module = "Audit",
            Category = "Compliance",
            Direction = EventDirection.Downstream,
            Description = "Triggered when a compliance policy is updated. Payload: { policyId: Guid, updatedBy: Guid, changes: string[] }"
        });
    }
}

/// <summary>
/// Type-safe event name constants for Audit module.
/// Use these instead of magic strings for IntelliSense support.
/// </summary>
public static class AuditEvents
{
    /// <summary>
    /// AuditLog-related events.
    /// </summary>
    public static class AuditLog
    {
        public const string Created = "auditlog.created";
        public const string Viewed = "auditlog.viewed";
        public const string Exported = "auditlog.exported";
    }
    
    /// <summary>
    /// AuditEntry-related events.
    /// </summary>
    public static class AuditEntry
    {
        public const string Recorded = "auditentry.recorded";
        public const string Updated = "auditentry.updated";
    }
    
    /// <summary>
    /// EntityChange-related events.
    /// </summary>
    public static class EntityChange
    {
        public const string Tracked = "entitychange.tracked";
        public const string PropertyChanged = "entitychange.property.changed";
    }
    
    /// <summary>
    /// Compliance-related events.
    /// </summary>
    public static class Compliance
    {
        public const string ReportGenerated = "compliance.report.generated";
        public const string ViolationDetected = "compliance.violation.detected";
        public const string PolicyUpdated = "compliance.policy.updated";
    }
}

