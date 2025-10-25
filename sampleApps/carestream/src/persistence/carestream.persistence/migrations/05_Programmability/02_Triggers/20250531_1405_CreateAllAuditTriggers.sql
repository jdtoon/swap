-- 20250531_1405_CreateAllAuditTriggers.sql
-- Creates AFTER INSERT/UPDATE/DELETE triggers for all auditable tables.

SET search_path TO app, public;

DO $$
DECLARE
    t text;
BEGIN
    FOR t IN SELECT table_name FROM information_schema.tables WHERE table_schema = 'app'
                                                              AND table_name NOT IN ('audit_log', 'schema_versions') -- Exclude audit log from auditing itself, and DbUp tracking table
    LOOP
        EXECUTE format('
            CREATE OR REPLACE TRIGGER trg_audit_%I
            AFTER INSERT OR UPDATE OR DELETE ON app.%I
            FOR EACH ROW EXECUTE FUNCTION app.audit_trigger_function();
        ', t, t);
    END LOOP;
END $$;