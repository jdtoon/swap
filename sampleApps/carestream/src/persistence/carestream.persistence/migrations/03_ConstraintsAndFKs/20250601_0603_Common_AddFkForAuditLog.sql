-- 20250601_0603_Common_AddFkForAuditLog.sql
-- Adds foreign key constraints for the app.audit_log table.

SET search_path TO app, public;

ALTER TABLE audit_log
    ADD CONSTRAINT fk_audit_log_audited_by_user_id FOREIGN KEY (audited_by_user_id) REFERENCES users(user_id),
    ADD CONSTRAINT fk_audit_log_facility_id FOREIGN KEY (facility_id) REFERENCES facilities(facility_id);