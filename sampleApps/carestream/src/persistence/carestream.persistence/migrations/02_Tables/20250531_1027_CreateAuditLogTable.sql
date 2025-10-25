-- 20250531_1027_CreateAuditLogTable.sql
-- Creates a comprehensive audit log table.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS audit_log (
    audit_log_id BIGSERIAL PRIMARY KEY,
    audited_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    audited_by_user_id INT NULL,
    facility_id INT NULL,
    action_type VARCHAR(20) NOT NULL,
    table_name VARCHAR(100) NULL,
    record_id TEXT NULL,
    old_data JSONB NULL,
    new_data JSONB NULL,
    changed_fields JSONB NULL,
    ip_address VARCHAR(100) NULL,
    user_agent TEXT NULL,
    notes TEXT NULL,
    is_system_action BOOLEAN NOT NULL DEFAULT FALSE
);