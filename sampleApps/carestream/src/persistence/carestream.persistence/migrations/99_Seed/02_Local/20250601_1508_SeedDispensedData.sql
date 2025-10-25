-- 20250601_1508_SeedDispensedData.sql
-- Seeds dispensed medication data for pharmacy history.

SET search_path TO app, public;

-- Get the visit_id for the 'PendingPrescription' visit for PTE10000001
-- This is Visit 8 from 20250601_1505_SeedPatientVisits.sql
DO $$
DECLARE
    v_target_visit_id INT;
    v_patient_id INT;
BEGIN
    SELECT patient_id INTO v_patient_id FROM patients WHERE force_number = 'PTE10000001';

    SELECT visit_id INTO v_target_visit_id
    FROM visits
    WHERE patient_id = v_patient_id AND status = 'PendingPrescription'
    ORDER BY visit_timestamp DESC LIMIT 1;

    -- Update prescription items for this visit
    UPDATE prescription_items
    SET quantity_dispensed = '20', is_fully_dispensed = TRUE, last_dispensed_at = NOW() - INTERVAL '1 day', last_dispensed_by_user_id = (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_pharmacist_1')
    WHERE visit_id = v_target_visit_id
      AND medication_id = (SELECT medication_id FROM medications WHERE name = 'Lisinopril' AND strength = '10mg');

    UPDATE prescription_items
    SET quantity_dispensed = '21', is_fully_dispensed = TRUE, last_dispensed_at = NOW() - INTERVAL '1 day', last_dispensed_by_user_id = (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_pharmacist_1')
    WHERE visit_id = v_target_visit_id
      AND medication_id = (SELECT medication_id FROM medications WHERE name = 'Amoxicillin' AND strength = '500mg');

    -- Insert dispensation log items
    INSERT INTO dispensation_log_items (prescription_item_id, visit_id, medication_id, quantity_dispensed_transaction, dispensed_at, dispensed_by_user_id, pharmacist_notes) VALUES
    ((SELECT prescription_item_id FROM prescription_items WHERE visit_id = v_target_visit_id AND medication_id = (SELECT medication_id FROM medications WHERE name = 'Lisinopril' AND strength = '10mg')),
     v_target_visit_id,
     (SELECT medication_id FROM medications WHERE name = 'Lisinopril' AND strength = '10mg'),
     '20', NOW() - INTERVAL '1 day', (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_pharmacist_1'), 'Dispensed Lisinopril 20 tablets.'),
    ((SELECT prescription_item_id FROM prescription_items WHERE visit_id = v_target_visit_id AND medication_id = (SELECT medication_id FROM medications WHERE name = 'Amoxicillin' AND strength = '500mg')),
     v_target_visit_id,
     (SELECT medication_id FROM medications WHERE name = 'Amoxicillin' AND strength = '500mg'),
     '21', NOW() - INTERVAL '1 day', (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_pharmacist_1'), 'Dispensed Amoxicillin full course.');

    -- Optionally, update the visit status to reflect completion of prescription if all items are dispensed
    -- This assumes that "Discharged" implies all medical actions including prescription are done.
    -- If "Pending Prescription" is a final status, this might not be needed.
    -- For seed data and demonstration, making it 'Discharged' or 'Completed' is useful.
    UPDATE visits
    SET status = 'Discharged' -- Or 'Completed' if you have such a status
    WHERE visit_id = v_target_visit_id;

END $$;