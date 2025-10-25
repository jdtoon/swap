-- 20250603_1601_SeedSuperAdminWorkflowData.sql
-- Comprehensive workflow data linked to the Super Admin user.

SET search_path TO app, public;

DO $$
DECLARE
    super_admin_user_id INT := (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_superadmin_001');
    patient_john_id INT := (SELECT patient_id FROM patients WHERE force_number = 'PTE10000001'); -- Sarah Johnson
    patient_mike_id INT := (SELECT patient_id FROM patients WHERE force_number = 'SGT10000002'); -- Michael Chen
    facility_2mh_id INT := (SELECT facility_id FROM facilities WHERE short_code = '2MH');
    facility_1mh_id INT := (SELECT facility_id FROM facilities WHERE short_code = '1MH');
    visit_id_super_1 INT; -- For check-in, vitals, consult, prescription, dispense
    visit_id_super_2 INT; -- For admin close & new
    med_amox_id INT := (SELECT medication_id FROM medications WHERE name = 'Amoxicillin');
    med_ibu_id INT := (SELECT medication_id FROM medications WHERE name = 'Ibuprofen');
    icd10_j069_id INT := (SELECT icd10_code_id FROM icd10_codes WHERE code = 'J06.9');
    proc_basic_id INT := (SELECT procedure_id FROM procedures WHERE code = 'PROC001');
    presc_item_id_1 INT;
    presc_item_id_2 INT;
BEGIN
    RAISE NOTICE 'Seeding Super Admin Workflow Data for user ID: %', super_admin_user_id;

    -- 1. Patient Check-in (Super Admin as checked_in_by_user_id)
    INSERT INTO visits (patient_id, brief_reason, status, checked_in_by_user_id, facility_id, visit_timestamp)
    VALUES (patient_john_id, 'Severe cough, fever (Super Admin Check-in)', 'WaitingForVitals', super_admin_user_id, facility_2mh_id, NOW() - INTERVAL '6 hours')
    RETURNING visit_id INTO visit_id_super_1;

    RAISE NOTICE 'Created Visit ID % for Super Admin workflow (check-in).', visit_id_super_1;

    -- 2. Vitals Capture (Super Admin as recorded_by_user_id)
    INSERT INTO vital_signs (visit_id, patient_id, blood_pressure_systolic, blood_pressure_diastolic, heart_rate, temperature, respiratory_rate, oxygen_saturation, pain_level, clinical_notes, recorded_at, recorded_by_user_id)
    VALUES (visit_id_super_1, patient_john_id, 125, 85, 78, 37.5, 18, 97, 5, 'Patient presents with persistent cough and low-grade fever. SA vitals.', NOW() - INTERVAL '5 hours 30 minutes', super_admin_user_id);

    -- Update Visit status to ReadyForDoctor (Super Admin as assigned_officer_user_id and updated_by_user_id)
    UPDATE visits
    SET status = 'ReadyForDoctor', assigned_officer_user_id = super_admin_user_id, updated_at = NOW()
    WHERE visit_id = visit_id_super_1;

    RAISE NOTICE 'Vitals captured and visit status updated to ReadyForDoctor by Super Admin for Visit ID %.', visit_id_super_1;

    -- 3. Consultation Actions (Super Admin as assigned_officer_user_id, created_by_user_id)
    -- Update to ConsultationInProgress (Simulating doctor starting)
    UPDATE visits
    SET status = 'ConsultationInProgress', updated_at = NOW()
    WHERE visit_id = visit_id_super_1;

    -- Add Diagnosis
    INSERT INTO visit_diagnoses (visit_id, icd10_code_id, diagnosis_type, recorded_at, recorded_by_user_id)
    VALUES (visit_id_super_1, icd10_j069_id, 'Primary', NOW() - INTERVAL '4 hours 45 minutes', super_admin_user_id);

    -- Add Procedure
    INSERT INTO visit_procedures (visit_id, procedure_id, performed_at, performed_by_user_id, notes)
    VALUES (visit_id_super_1, proc_basic_id, NOW() - INTERVAL '4 hours 40 minutes', super_admin_user_id, 'Comprehensive physical exam by SA.');

    -- Add Doctor Notes
    UPDATE visits
    SET doctor_consultation_notes = 'Patient examined, diagnosed with URI. Prescribed antibiotics and rest. SA notes.', updated_at = NOW()
    WHERE visit_id = visit_id_super_1;

    -- 4. Prescription (Super Admin as created_by_user_id)
    INSERT INTO prescription_items (visit_id, medication_id, dosage, frequency, duration, quantity_prescribed, special_instructions, created_by_user_id, is_sent_to_pharmacy, pharmacy_sent_at)
    VALUES (visit_id_super_1, med_amox_id, '500mg', 'TID', '7 days', '21', 'Take with food. SA prescribed.', super_admin_user_id, TRUE, NOW() - INTERVAL '4 hours 30 minutes')
    RETURNING prescription_item_id INTO presc_item_id_1;

    INSERT INTO prescription_items (visit_id, medication_id, dosage, frequency, quantity_prescribed, special_instructions, created_by_user_id, is_sent_to_pharmacy, pharmacy_sent_at)
    VALUES (visit_id_super_1, med_ibu_id, '400mg', 'PRN', '10', 'For pain relief. SA prescribed.', super_admin_user_id, TRUE, NOW() - INTERVAL '4 hours 30 minutes')
    RETURNING prescription_item_id INTO presc_item_id_2;

    -- Update Visit status to PendingPrescription
    UPDATE visits
    SET status = 'PendingPrescription', updated_at = NOW()
    WHERE visit_id = visit_id_super_1;

    RAISE NOTICE 'Prescriptions created and visit status updated to PendingPrescription by Super Admin for Visit ID %.', visit_id_super_1;

    -- 5. Dispensation (Super Admin as dispensed_by_user_id)
    -- Update stock first
    UPDATE medication_stock
    SET quantity_on_hand = quantity_on_hand - 21, last_updated_at = NOW()
    WHERE medication_id = med_amox_id AND facility_id = facility_2mh_id;
    UPDATE medication_stock
    SET quantity_on_hand = quantity_on_hand - 10, last_updated_at = NOW()
    WHERE medication_id = med_ibu_id AND facility_id = facility_2mh_id;

    -- Log dispense action
    INSERT INTO dispensation_log_items (prescription_item_id, visit_id, medication_id, quantity_dispensed_transaction, dispensed_by_user_id, pharmacist_notes, dispensed_at)
    VALUES (presc_item_id_1, visit_id_super_1, med_amox_id, '21', super_admin_user_id, 'Dispensed full course of Amoxicillin. SA dispensed.', NOW() - INTERVAL '4 hours');

    INSERT INTO dispensation_log_items (prescription_item_id, visit_id, medication_id, quantity_dispensed_transaction, dispensed_by_user_id, pharmacist_notes, dispensed_at)
    VALUES (presc_item_id_2, visit_id_super_1, med_ibu_id, '10', super_admin_user_id, 'Dispensed 10 Ibuprofen. SA dispensed.', NOW() - INTERVAL '4 hours');

    -- Update prescription items as fully dispensed
    UPDATE prescription_items
    SET is_fully_dispensed = TRUE, quantity_dispensed = quantity_prescribed, last_dispensed_at = NOW() - INTERVAL '4 hours', last_dispensed_by_user_id = super_admin_user_id
    WHERE prescription_item_id = presc_item_id_1 OR prescription_item_id = presc_item_id_2;

    -- Update Visit status to Discharged
    UPDATE visits
    SET status = 'Discharged', updated_at = NOW()
    WHERE visit_id = visit_id_super_1;

    RAISE NOTICE 'Prescriptions dispensed and visit status updated to Discharged by Super Admin for Visit ID %.', visit_id_super_1;

    -- 6. Sick Note (Super Admin as issued_by_user_id)
    INSERT INTO sick_notes (visit_id, start_date, end_date, diagnosis, recommendations, issued_at, issued_by_user_id)
    VALUES (visit_id_super_1, NOW()::DATE, (NOW() + INTERVAL '3 days')::DATE, 'Acute Viral Syndrome', 'Rest for 3 days. SA issued.', NOW() - INTERVAL '3 hours', super_admin_user_id);

    RAISE NOTICE 'Sick Note issued by Super Admin for Visit ID %.', visit_id_super_1;

    -- 7. Staff Report (Super Admin as author_user_id)
    INSERT INTO staff_reports (author_user_id, title, priority, content, created_at, facility_id, department_id)
    VALUES (super_admin_user_id, 'System Update - Phase 1 Complete', 'High', 'All core modules have been tested and verified. Ready for next phase. SA Report.', NOW() - INTERVAL '1 hour', facility_2mh_id, NULL);

    RAISE NOTICE 'Staff Report created by Super Admin.';

    -- 8. Admin Action - Create new Department (Super Admin as created_by_user_id)
    INSERT INTO departments (facility_id, name, description, is_active, created_at, created_by_user_id)
    VALUES (facility_1mh_id, 'Cardiology (SA Created)', 'Department created by Super Admin.', TRUE, NOW(), super_admin_user_id);

    RAISE NOTICE 'Department created by Super Admin.';

    -- 9. Admin Action - Update a Facility (Super Admin as updated_by_user_id)
    UPDATE facilities
    SET phone_number = '0000000000', updated_at = NOW(), updated_by_user_id = super_admin_user_id
    WHERE facility_id = facility_1mh_id;

    RAISE NOTICE 'Facility updated by Super Admin.';

    -- 10. Admin Close and Start New Visit for another patient (Super Admin as performing user)
    INSERT INTO visits (patient_id, brief_reason, status, checked_in_by_user_id, facility_id, visit_timestamp)
    VALUES (patient_mike_id, 'Follow-up appointment (Super Admin Check-in)', 'WaitingForVitals', super_admin_user_id, facility_2mh_id, NOW() - INTERVAL '30 minutes')
    RETURNING visit_id INTO visit_id_super_2;

    RAISE NOTICE 'Created Visit ID % for Super Admin workflow (close and start new).', visit_id_super_2;

    -- Simulate the administrative close
    UPDATE visits
    SET status = 'AdministrativelyClosed', updated_at = NOW()
    WHERE visit_id = (SELECT visit_id FROM visits WHERE patient_id = patient_mike_id AND status = 'WaitingForVitals' AND visit_id != visit_id_super_2 ORDER BY visit_timestamp DESC LIMIT 1);
    -- This update is highly dependent on prior visits for Mike. Ensure Mike has an existing visit for this to simulate correctly.
    -- For robust seeding, you might need to insert an 'old' visit for Mike specifically before this.
    -- For simplicity, assuming the target for closure exists.

END $$;