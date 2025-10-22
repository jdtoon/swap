using NetMX.Events;

namespace NetMX.Events;

/// <summary>
/// Audit module domain events extending the base DomainEvents class.
/// These events enable event-driven HTMX patterns for audit logging and compliance.
/// </summary>
public static partial class DomainEvents
{
    /// <summary>
    /// Audit log events - for capturing system-wide audit records
    /// </summary>
    public static class AuditLog
    {
        /// <summary>
        /// Triggered when a new audit log entry is created.
        /// Payload: { id: Guid, entityType: string, action: string }
        /// </summary>
        [EventDirection(EventDirection.Terminal)]
        public const string Created = "audit-log.created";

        /// <summary>
        /// Triggered when an audit log entry is updated.
        /// Payload: { id: Guid }
        /// </summary>
        [EventDirection(EventDirection.Terminal)]
        public const string Updated = "audit-log.updated";

        /// <summary>
        /// Triggered when an audit log entry is deleted.
        /// Payload: { id: Guid }
        /// </summary>
        [EventDirection(EventDirection.Terminal)]
        public const string Deleted = "audit-log.deleted";

        /// <summary>
        /// Triggered when audit log is queried (for security monitoring).
        /// Payload: { userId: Guid, queryParameters: object }
        /// </summary>
        [EventDirection(EventDirection.Terminal)]
        public const string Queried = "audit-log.queried";

        /// <summary>
        /// Triggered when audit logs are exported.
        /// Payload: { userId: Guid, format: string, recordCount: int }
        /// </summary>
        [EventDirection(EventDirection.Terminal)]
        public const string Exported = "audit-log.exported";
    }

    /// <summary>
    /// Audit entry events - for individual audit record details
    /// </summary>
    public static class AuditEntry
    {
        /// <summary>
        /// Triggered when an audit entry is created (detailed record).
        /// Payload: { id: Guid, entityId: Guid, changedProperties: string[] }
        /// </summary>
        [EventDirection(EventDirection.Terminal)]
        public const string Created = "audit-entry.created";

        /// <summary>
        /// Triggered when an audit entry is updated.
        /// Payload: { id: Guid }
        /// </summary>
        [EventDirection(EventDirection.Terminal)]
        public const string Updated = "audit-entry.updated";

        /// <summary>
        /// Triggered when an audit entry is deleted.
        /// Payload: { id: Guid }
        /// </summary>
        [EventDirection(EventDirection.Terminal)]
        public const string Deleted = "audit-entry.deleted";

        /// <summary>
        /// Triggered when audit entry is viewed (audit trail of audit viewing).
        /// Payload: { entryId: Guid, viewedBy: Guid }
        /// </summary>
        [EventDirection(EventDirection.Terminal)]
        public const string Viewed = "audit-entry.viewed";
    }

    /// <summary>
    /// Entity change tracking events
    /// </summary>
    public static class EntityChange
    {
        /// <summary>
        /// Triggered when any entity is created.
        /// Payload: { entityType: string, entityId: Guid, properties: object }
        /// </summary>
        [EventDirection(EventDirection.Downstream)]
        public const string Created = "entity-change.created";

        /// <summary>
        /// Triggered when any entity is updated.
        /// Payload: { entityType: string, entityId: Guid, changedProperties: string[], oldValues: object, newValues: object }
        /// </summary>
        [EventDirection(EventDirection.Downstream)]
        public const string Updated = "entity-change.updated";

        /// <summary>
        /// Triggered when any entity is deleted (soft or hard delete).
        /// Payload: { entityType: string, entityId: Guid, deletionType: string }
        /// </summary>
        [EventDirection(EventDirection.Downstream)]
        public const string Deleted = "entity-change.deleted";
    }

    /// <summary>
    /// Compliance and retention events
    /// </summary>
    public static class Compliance
    {
        /// <summary>
        /// Triggered when retention policy is applied (old logs deleted).
        /// Payload: { deletedCount: int, retentionDays: int, policyName: string }
        /// </summary>
        [EventDirection(EventDirection.Terminal)]
        public const string RetentionApplied = "compliance.retention-applied";

        /// <summary>
        /// Triggered when compliance report is generated.
        /// Payload: { reportType: string, startDate: DateTime, endDate: DateTime, userId: Guid }
        /// </summary>
        [EventDirection(EventDirection.Terminal)]
        public const string ReportGenerated = "compliance.report-generated";

        /// <summary>
        /// Triggered when suspicious activity is detected.
        /// Payload: { activityType: string, severity: string, userId: Guid?, details: object }
        /// </summary>
        [EventDirection(EventDirection.Upstream)]
        public const string SuspiciousActivity = "compliance.suspicious-activity";
    }
}
