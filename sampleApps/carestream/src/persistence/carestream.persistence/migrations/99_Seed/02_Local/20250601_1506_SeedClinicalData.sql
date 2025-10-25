-- 20250601_1506_SeedClinicalData.sql
-- Seeds various clinical data for existing visits and patients.

SET search_path TO app, public;

-- Vitals for Visit 2 (Michael Chen, Ready for Doctor)
INSERT INTO vital_signs (visit_id, patient_id, blood_pressure_systolic, blood_pressure_diastolic, heart_rate, temperature, respiratory_rate, oxygen_saturation, pain_level, urinalysis_color, urinalysis_clarity, urinalysis_specific_gravity, urinalysis_ph, urinalysis_protein, urinalysis_glucose, clinical_notes, requires_follow_up, mark_as_urgent, recorded_at, recorded_by_user_id) VALUES
((SELECT visit_id FROM visits WHERE patient_id = (SELECT patient_id FROM patients WHERE force_number = 'SGT10000002') AND status = 'ReadyForDoctor' ORDER BY visit_timestamp DESC LIMIT 1),
 (SELECT patient_id FROM patients WHERE force_number = 'SGT10000002'), 120, 80, 72, 36.6, 16, 98, 3, 'Yellow', 'Clear', 1.015, 6.0, 'Negative', 'Negative', 'Patient stable, minor discomfort on movement.', FALSE, FALSE, NOW() - INTERVAL '40 minutes', (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_nurse_1'));

-- Vitals for Visit 6 (PTE10000001, Discharged) - Historical Vitals
INSERT INTO vital_signs (visit_id, patient_id, blood_pressure_systolic, blood_pressure_diastolic, heart_rate, temperature, respiratory_rate, oxygen_saturation, pain_level, clinical_notes, recorded_at, recorded_by_user_id) VALUES
((SELECT visit_id FROM visits WHERE patient_id = (SELECT patient_id FROM patients WHERE force_number = 'PTE10000001') AND status = 'Discharged' ORDER BY visit_timestamp DESC LIMIT 1),
 (SELECT patient_id FROM patients WHERE force_number = 'PTE10000001'), 110, 70, 68, 37.0, 14, 99, 0, 'Initial assessment for flu symptoms. Patient resting.', NOW() - INTERVAL '2 days 6 hours', (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_nurse_1')),
((SELECT visit_id FROM visits WHERE patient_id = (SELECT patient_id FROM patients WHERE force_number = 'PTE10000001') AND status = 'Discharged' ORDER BY visit_timestamp DESC LIMIT 1),
 (SELECT patient_id FROM patients WHERE force_number = 'PTE10000001'), 115, 75, 70, 36.8, 15, 99, 0, 'Follow-up vitals before discharge.', NOW() - INTERVAL '2 days 2 hours', (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_nurse_1'));

-- Patient Medical History (Sarah Johnson)
INSERT INTO patient_medical_history (patient_id, type, description, onset_date, severity, notes, recorded_at, recorded_by_user_id, is_active) VALUES
((SELECT patient_id FROM patients WHERE force_number = 'PTE10000001'), 'Condition', 'Hypertension', '2015-01-01', 'Moderate', 'Managed with Lisinopril', NOW(), (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'), TRUE),
((SELECT patient_id FROM patients WHERE force_number = 'PTE10000001'), 'Allergy', 'Penicillin', '2000-01-01', 'Severe', 'Anaphylaxis reaction previously', NOW(), (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'), TRUE);

-- Visit Assessments (for Michael Chen's 'ReadyForDoctor' visit)
INSERT INTO visit_assessments (visit_id, patient_id, assessment_date, assessed_by_user_id, physical_exam_findings, cardiovascular_notes, musculoskeletal_notes, medical_classification, deployment_status, validity_period_months) VALUES
((SELECT visit_id FROM visits WHERE patient_id = (SELECT patient_id FROM patients WHERE force_number = 'SGT10000002') AND status = 'ReadyForDoctor' ORDER BY visit_timestamp DESC LIMIT 1),
 (SELECT patient_id FROM patients WHERE force_number = 'SGT10000002'), NOW() - INTERVAL '35 minutes', (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'),
 'Mild swelling left ankle. No significant discoloration. Full ROM limited by pain.', 'S1/S2 normal, no murmurs.', 'Pain on inversion/eversion of left ankle.', 'Temporary Restricted Duty', 'Fit', 6);

-- Visit Diagnoses (for Michael Chen's 'ReadyForDoctor' visit)
INSERT INTO visit_diagnoses (visit_id, icd10_code_id, diagnosis_type, recorded_at, recorded_by_user_id) VALUES
((SELECT visit_id FROM visits WHERE patient_id = (SELECT patient_id FROM patients WHERE force_number = 'SGT10000002') AND status = 'ReadyForDoctor' ORDER BY visit_timestamp DESC LIMIT 1),
 (SELECT icd10_code_id FROM icd10_codes WHERE code = 'S93.401A'), 'Primary', NOW() - INTERVAL '30 minutes', (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1')),
((SELECT visit_id FROM visits WHERE patient_id = (SELECT patient_id FROM patients WHERE force_number = 'SGT10000002') AND status = 'ReadyForDoctor' ORDER BY visit_timestamp DESC LIMIT 1),
 (SELECT icd10_code_id FROM icd10_codes WHERE code = 'M54.5'), 'Secondary', NOW() - INTERVAL '30 minutes', (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'));

-- Visit Procedures (for Michael Chen's 'ReadyForDoctor' visit)
INSERT INTO visit_procedures (visit_id, procedure_id, performed_at, performed_by_user_id, notes) VALUES
((SELECT visit_id FROM visits WHERE patient_id = (SELECT patient_id FROM patients WHERE force_number = 'SGT10000002') AND status = 'ReadyForDoctor' ORDER BY visit_timestamp DESC LIMIT 1),
 (SELECT procedure_id FROM procedures WHERE code = 'PROC001'), NOW() - INTERVAL '35 minutes', (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'), 'General physical examination completed.');