-- 20250531_1400_CreateAllTimestampTriggers.sql
-- Creates BEFORE UPDATE triggers to automatically update 'updated_at' columns for relevant tables.

SET search_path TO app, public;

DO $$
BEGIN
    -- Tables with an 'updated_at' column
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_users_update_timestamp' AND tgrelid = 'app.users'::regclass) THEN
        CREATE TRIGGER trg_users_update_timestamp BEFORE UPDATE ON app.users FOR EACH ROW EXECUTE FUNCTION app.fn_update_timestamp();
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_patients_update_timestamp' AND tgrelid = 'app.patients'::regclass) THEN
        CREATE TRIGGER trg_patients_update_timestamp BEFORE UPDATE ON app.patients FOR EACH ROW EXECUTE FUNCTION app.fn_update_timestamp();
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_facilities_update_timestamp' AND tgrelid = 'app.facilities'::regclass) THEN
        CREATE TRIGGER trg_facilities_update_timestamp BEFORE UPDATE ON app.facilities FOR EACH ROW EXECUTE FUNCTION app.fn_update_timestamp();
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_departments_update_timestamp' AND tgrelid = 'app.departments'::regclass) THEN
        CREATE TRIGGER trg_departments_update_timestamp BEFORE UPDATE ON app.departments FOR EACH ROW EXECUTE FUNCTION app.fn_update_timestamp();
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_wards_update_timestamp' AND tgrelid = 'app.wards'::regclass) THEN
        CREATE TRIGGER trg_wards_update_timestamp BEFORE UPDATE ON app.wards FOR EACH ROW EXECUTE FUNCTION app.fn_update_timestamp();
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_sick_notes_update_timestamp' AND tgrelid = 'app.sick_notes'::regclass) THEN
        CREATE TRIGGER trg_sick_notes_update_timestamp BEFORE UPDATE ON app.sick_notes FOR EACH ROW EXECUTE FUNCTION app.fn_update_timestamp();
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_icd10_codes_update_timestamp' AND tgrelid = 'app.icd10_codes'::regclass) THEN
        CREATE TRIGGER trg_icd10_codes_update_timestamp BEFORE UPDATE ON app.icd10_codes FOR EACH ROW EXECUTE FUNCTION app.fn_update_timestamp();
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_procedures_update_timestamp' AND tgrelid = 'app.procedures'::regclass) THEN
        CREATE TRIGGER trg_procedures_update_timestamp BEFORE UPDATE ON app.procedures FOR EACH ROW EXECUTE FUNCTION app.fn_update_timestamp();
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_referrals_update_timestamp' AND tgrelid = 'app.referrals'::regclass) THEN
        CREATE TRIGGER trg_referrals_update_timestamp BEFORE UPDATE ON app.referrals FOR EACH ROW EXECUTE FUNCTION app.fn_update_timestamp();
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_conversations_update_timestamp' AND tgrelid = 'app.conversations'::regclass) THEN
        CREATE TRIGGER trg_conversations_update_timestamp BEFORE UPDATE ON app.conversations FOR EACH ROW EXECUTE FUNCTION app.fn_update_timestamp();
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_roles_update_timestamp' AND tgrelid = 'app.roles'::regclass) THEN
        CREATE TRIGGER trg_roles_update_timestamp BEFORE UPDATE ON app.roles FOR EACH ROW EXECUTE FUNCTION app.fn_update_timestamp();
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_patient_medical_history_update_timestamp' AND tgrelid = 'app.patient_medical_history'::regclass) THEN
        CREATE TRIGGER trg_patient_medical_history_update_timestamp BEFORE UPDATE ON app.patient_medical_history FOR EACH ROW EXECUTE FUNCTION app.fn_update_timestamp();
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_visit_assessments_update_timestamp' AND tgrelid = 'app.visit_assessments'::regclass) THEN
        CREATE TRIGGER trg_visit_assessments_update_timestamp BEFORE UPDATE ON app.visit_assessments FOR EACH ROW EXECUTE FUNCTION app.fn_update_timestamp();
    END IF;

    -- For tables with specific timestamp columns like `last_updated_at`, consider separate triggers/functions if needed.
    -- For example, for medication_stock:
    -- CREATE OR REPLACE FUNCTION app.fn_update_medication_stock_timestamp() RETURNS TRIGGER AS $$ BEGIN NEW.last_updated_at = NOW(); RETURN NEW; END; $$ LANGUAGE plpgsql;
    -- CREATE TRIGGER trg_medication_stock_last_updated BEFORE UPDATE ON app.medication_stock FOR EACH ROW EXECUTE FUNCTION app.fn_update_medication_stock_timestamp();
    -- Or manage `last_updated_at` from application code.
END $$;