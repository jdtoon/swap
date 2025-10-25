-- 20250601_1402_Common_AddIdxForAuditLog.sql
-- Adds indexes for the audit log.

SET search_path TO app, public;

CREATE INDEX ix_audit_log_audited_by_user_id ON audit_log (audited_by_user_id);
CREATE INDEX ix_audit_log_facility_id ON audit_log (facility_id);
CREATE INDEX ix_audit_log_action_type ON audit_log (action_type);
CREATE INDEX ix_audit_log_audited_at ON audit_log (audited_at DESC);