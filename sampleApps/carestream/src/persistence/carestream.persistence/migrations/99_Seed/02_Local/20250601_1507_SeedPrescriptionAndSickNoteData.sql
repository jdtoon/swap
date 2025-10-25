-- 20250601_1507_SeedPrescriptionAndSickNoteData.sql
-- Seeds prescription items and sick note data.

SET search_path TO app, public;

-- Prescription Items for Visit 8 (PTE10000001, Pending Prescription)
-- This simulates a doctor prescribing medication that is now in the pharmacy queue.
INSERT INTO prescription_items (visit_id, medication_id, dosage, frequency, duration, quantity_prescribed, special_instructions, is_sent_to_pharmacy, pharmacy_sent_at, created_at, created_by_user_id) VALUES
((SELECT visit_id FROM visits WHERE patient_id = (SELECT patient_id FROM patients WHERE force_number = 'PTE10000001') AND status = 'PendingPrescription' ORDER BY visit_timestamp DESC LIMIT 1),
 (SELECT medication_id FROM medications WHERE name = 'Lisinopril' AND strength = '10mg'), '10mg', 'Once daily', '90 days', '90', 'Take with food', TRUE, NOW() - INTERVAL '30 minutes', NOW() - INTERVAL '35 minutes', (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1')),
((SELECT visit_id FROM visits WHERE patient_id = (SELECT patient_id FROM patients WHERE force_number = 'PTE10000001') AND status = 'PendingPrescription' ORDER BY visit_timestamp DESC LIMIT 1),
 (SELECT medication_id FROM medications WHERE name = 'Amoxicillin' AND strength = '500mg'), '500mg', 'Three times daily', '7 days', '21', 'Complete full course', TRUE, NOW() - INTERVAL '30 minutes', NOW() - INTERVAL '35 minutes', (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'));

-- Sick Note for a past visit (Michael Chen)
INSERT INTO sick_notes (visit_id, start_date, end_date, diagnosis, recommendations, issued_at, issued_by_user_id) VALUES
((SELECT visit_id FROM visits WHERE patient_id = (SELECT patient_id FROM patients WHERE force_number = 'SGT10000002') AND status = 'Discharged' ORDER BY visit_timestamp ASC LIMIT 1), -- Oldest visit for Michael Chen
 '2024-05-28', '2024-05-30', 'Acute upper respiratory infection', 'Rest, hydrate, avoid strenuous activity.', NOW() - INTERVAL '3 days', (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'));

 INSERT INTO sick_notes (visit_id, start_date, end_date, diagnosis, recommendations, issued_at, issued_by_user_id) VALUES
((SELECT visit_id FROM visits WHERE patient_id = (SELECT patient_id FROM patients WHERE force_number = 'PTE10000001') AND status = 'Discharged' ORDER BY visit_timestamp ASC LIMIT 1), -- Oldest discharged visit for Sarah Johnson
 '2024-05-28', '2024-05-30', 'Acute upper respiratory infection', 'Rest, hydrate, avoid strenuous activity.', NOW() - INTERVAL '3 days', (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'));