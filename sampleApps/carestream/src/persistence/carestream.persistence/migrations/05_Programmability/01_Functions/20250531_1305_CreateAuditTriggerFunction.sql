-- 20250531_1305_CreateAuditTriggerFunction.sql
-- Creates a generic audit trigger function to log data changes.

SET search_path TO app, public;

CREATE OR REPLACE FUNCTION app.audit_trigger_function()
RETURNS TRIGGER AS $$
DECLARE
    v_old_data JSONB;
    v_new_data JSONB;
    v_changed_fields JSONB;
    v_pk_value TEXT;
    v_pk_column_name TEXT;
    v_audit_user_id INT;
    v_audit_facility_id INT;
BEGIN
    -- Safely get current user and facility context
    BEGIN
        v_audit_user_id := current_setting('app.current_user_id', true)::INT;
    EXCEPTION
        WHEN OTHERS THEN v_audit_user_id := NULL;
    END;

    BEGIN
        v_audit_facility_id := current_setting('app.current_facility_id', true)::INT;
    EXCEPTION
        WHEN OTHERS THEN v_audit_facility_id := NULL;
    END;

    -- Determine the primary key column name for single-column PK tables
    -- Explicitly list all tables for clarity and safety, no fallback.
    -- Composite PK tables are handled separately below.
    CASE TG_TABLE_NAME
        WHEN 'users' THEN v_pk_column_name := 'user_id';
        WHEN 'patients' THEN v_pk_column_name := 'patient_id';
        WHEN 'visits' THEN v_pk_column_name := 'visit_id';
        WHEN 'vital_signs' THEN v_pk_column_name := 'vital_signs_id';
        WHEN 'staff_reports' THEN v_pk_column_name := 'report_id';
        WHEN 'medications' THEN v_pk_column_name := 'medication_id';
        WHEN 'prescription_items' THEN v_pk_column_name := 'prescription_item_id';
        WHEN 'sick_notes' THEN v_pk_column_name := 'sick_note_id';
        WHEN 'dispensation_log_items' THEN v_pk_column_name := 'dispensation_log_item_id';
        WHEN 'facilities' THEN v_pk_column_name := 'facility_id';
        WHEN 'departments' THEN v_pk_column_name := 'department_id';
        WHEN 'wards' THEN v_pk_column_name := 'ward_id';
        WHEN 'referrals' THEN v_pk_column_name := 'referral_id';
        WHEN 'icd10_codes' THEN v_pk_column_name := 'icd10_code_id';
        WHEN 'procedures' THEN v_pk_column_name := 'procedure_id';
        WHEN 'visit_diagnoses' THEN v_pk_column_name := 'visit_diagnosis_id';
        WHEN 'visit_procedures' THEN v_pk_column_name := 'visit_procedure_id';
        WHEN 'patient_medical_history' THEN v_pk_column_name := 'history_id';
        WHEN 'visit_assessments' THEN v_pk_column_name := 'visit_assessment_id';
        WHEN 'roles' THEN v_pk_column_name := 'role_id';
        WHEN 'audit_log' THEN v_pk_column_name := 'audit_log_id'; -- Still include as a case, even if excluded from actual trigger assignment
        WHEN 'messages' THEN v_pk_column_name := 'message_id'; -- Added
        -- Add any other single PK tables here if not already listed
        ELSE
            RAISE EXCEPTION 'Audit trigger function: Primary key column name not defined for table %', TG_TABLE_NAME;
    END CASE;

    -- Handle composite primary keys for v_pk_value
    IF TG_TABLE_NAME = 'user_facilities' THEN
        v_pk_value := (COALESCE(NEW.user_id, OLD.user_id)::TEXT) || '-' || (COALESCE(NEW.facility_id, OLD.facility_id)::TEXT);
    ELSIF TG_TABLE_NAME = 'medication_stock' THEN
        v_pk_value := (COALESCE(NEW.medication_id, OLD.medication_id)::TEXT) || '-' || (COALESCE(NEW.facility_id, OLD.facility_id)::TEXT);
    ELSIF TG_TABLE_NAME = 'conversation_participants' THEN
         v_pk_value := (COALESCE(NEW.conversation_id, OLD.conversation_id)::TEXT) || '-' || (COALESCE(NEW.user_id, OLD.user_id)::TEXT); -- Corrected based on discussion
    ELSIF TG_TABLE_NAME = 'patient_facility_visits_history' THEN
        v_pk_value := (COALESCE(NEW.patient_id, OLD.patient_id)::TEXT) || '-' || (COALESCE(NEW.facility_id, OLD.facility_id)::TEXT);
    ELSE
        -- For single-column PKs, dynamically access the column value
        IF TG_OP = 'INSERT' OR TG_OP = 'UPDATE' THEN
            EXECUTE format('SELECT ($1).%I::TEXT', v_pk_column_name) INTO v_pk_value USING NEW;
        ELSE -- TG_OP = 'DELETE'
            EXECUTE format('SELECT ($1).%I::TEXT', v_pk_column_name) INTO v_pk_value USING OLD;
        END IF;
    END IF;


    IF TG_OP = 'INSERT' THEN
        v_new_data := to_jsonb(NEW);
        INSERT INTO app.audit_log (audited_by_user_id, facility_id, action_type, table_name, record_id, new_data)
        VALUES (v_audit_user_id, v_audit_facility_id, 'INSERT', TG_TABLE_NAME, v_pk_value, v_new_data);
    ELSIF TG_OP = 'UPDATE' THEN
        v_old_data := to_jsonb(OLD);
        v_new_data := to_jsonb(NEW);

        SELECT jsonb_object_agg(key, jsonb_build_object('old', v_old_data->key, 'new', v_new_data->key))
        INTO v_changed_fields
        FROM jsonb_each(v_new_data)
        WHERE (v_new_data->key IS DISTINCT FROM v_old_data->key);

        -- Don't log if no actual changes (e.g., if only updated_at is changed by another trigger unless updated_at is important to track)
        IF v_changed_fields IS NULL THEN
            RETURN NEW; -- Or NULL for AFTER triggers
        END IF;

        INSERT INTO app.audit_log (audited_by_user_id, facility_id, action_type, table_name, record_id, old_data, new_data, changed_fields)
        VALUES (v_audit_user_id, v_audit_facility_id, 'UPDATE', TG_TABLE_NAME, v_pk_value, v_old_data, v_new_data, v_changed_fields);
    ELSIF TG_OP = 'DELETE' THEN
        v_old_data := to_jsonb(OLD);
        INSERT INTO app.audit_log (audited_by_user_id, facility_id, action_type, table_name, record_id, old_data)
        VALUES (v_audit_user_id, v_audit_facility_id, 'DELETE', TG_TABLE_NAME, v_pk_value, v_old_data);
    END IF;

    RETURN NULL; -- Important for AFTER triggers
END;
$$ LANGUAGE plpgsql;