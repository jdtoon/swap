-- 20250601_1505_SeedPatientVisits.sql
-- Seeds patient visits with various statuses to populate queues and dashboards.

SET search_path TO app, public;

-- Patient Admin Dashboard: Currently InTreatment (should be a visit that is not yet discharged)
-- Patient Admin Dashboard: Pending Check-in (not applicable, check-in creates the visit)
-- Patient Admin Dashboard: Total Sick Bay Visits (sum of all visits)
-- Nurse Dashboard: WaitingForVitals, VitalsInProgress, ReadyForDoctor

-- Visit 1: Sarah Johnson (PTE10000001) - InTreatment (VitalsInProgress)
-- Checked in by Sarah Mitchell (PatientAdmin 1)
-- Assigned to Emily Johnson (Nurse 1)
INSERT INTO visits (patient_id, brief_reason, status, checked_in_by_user_id, assigned_officer_user_id, facility_id, visit_timestamp) VALUES
((SELECT patient_id FROM patients WHERE force_number = 'PTE10000001'), 'Severe headache and fever', 'VitalsInProgress',
 (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_1'), (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_nurse_1'),
 (SELECT facility_id FROM facilities WHERE short_code = '2MH'), NOW() - INTERVAL '15 minutes');

-- Visit 2: Michael Chen (SGT10000002) - ReadyForDoctor (waiting in doctor queue)
-- Checked in by Sarah Mitchell (PatientAdmin 1)
-- Assigned to Themba Zulu (Doctor 1)
INSERT INTO visits (patient_id, brief_reason, status, checked_in_by_user_id, assigned_officer_user_id, facility_id, visit_timestamp) VALUES
((SELECT patient_id FROM patients WHERE force_number = 'SGT10000002'), 'Sprained ankle during training', 'ReadyForDoctor',
 (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_1'), (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'),
 (SELECT facility_id FROM facilities WHERE short_code = '2MH'), NOW() - INTERVAL '45 minutes');

-- Visit 3: Emily Rodriguez (CPL10000003) - WaitingForVitals (Nurse queue)
-- Checked in by Sarah Mitchell (PatientAdmin 1)
INSERT INTO visits (patient_id, brief_reason, status, checked_in_by_user_id, facility_id, visit_timestamp) VALUES
((SELECT patient_id FROM patients WHERE force_number = 'CPL10000003'), 'Respiratory issues', 'WaitingForVitals',
 (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_1'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), NOW() - INTERVAL '5 minutes');

-- Visit 4: Robert Williams (SSG10000004) - WaitingForVitals (Nurse queue, longer wait)
-- Checked in by Sarah Mitchell (PatientAdmin 1)
INSERT INTO visits (patient_id, brief_reason, status, checked_in_by_user_id, facility_id, visit_timestamp) VALUES
((SELECT patient_id FROM patients WHERE force_number = 'SSG10000004'), 'Lower back pain (urgent)', 'WaitingForVitals',
 (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_1'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), NOW() - INTERVAL '30 minutes');

-- Visit 5: Anna Dubois (CPT10000005) - ReadyForDoctor (another facility)
-- Checked in by John Rambo (PatientAdmin 2)
-- Assigned to Lisa Wong (Doctor 2)
INSERT INTO visits (patient_id, brief_reason, status, checked_in_by_user_id, assigned_officer_user_id, facility_id, visit_timestamp) VALUES
((SELECT patient_id FROM patients WHERE force_number = 'CPT10000005'), 'Routine check-up', 'ReadyForDoctor',
 (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_2'), (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_2'),
 (SELECT facility_id FROM facilities WHERE short_code = '1MH'), NOW() - INTERVAL '20 minutes');

-- Visit 6: Discharged Patient (for historical data)
INSERT INTO visits (patient_id, brief_reason, status, checked_in_by_user_id, assigned_officer_user_id, facility_id, visit_timestamp) VALUES
((SELECT patient_id FROM patients WHERE force_number = 'PTE10000001'), 'Follow-up for flu', 'Discharged',
 (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_1'), (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'),
 (SELECT facility_id FROM facilities WHERE short_code = '2MH'), NOW() - INTERVAL '2 days');

-- Visit 7: Active Patient, in treatment but not yet ready for doctor (for dashboard stats)
INSERT INTO visits (patient_id, brief_reason, status, checked_in_by_user_id, facility_id, visit_timestamp) VALUES
((SELECT patient_id FROM patients WHERE force_number = 'SGT10000002'), 'Ongoing treatment for skin rash', 'InTreatment',
 (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_1'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), NOW() - INTERVAL '1 hour');

-- Visit 8: Pending Prescription (for Pharmacy dashboard)
INSERT INTO visits (patient_id, brief_reason, status, checked_in_by_user_id, assigned_officer_user_id, facility_id, visit_timestamp) VALUES
((SELECT patient_id FROM patients WHERE force_number = 'PTE10000001'), 'Medication refill for hypertension', 'PendingPrescription',
 (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_1'), (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'),
 (SELECT facility_id FROM facilities WHERE short_code = '2MH'), NOW() - INTERVAL '40 minutes');

-- Visit 9: Another WaitingForVitals at 1MH
INSERT INTO visits (patient_id, brief_reason, status, checked_in_by_user_id, facility_id, visit_timestamp) VALUES
((SELECT patient_id FROM patients WHERE force_number = 'CPT10000005'), 'Nausea and dizziness', 'WaitingForVitals',
 (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_2'), (SELECT facility_id FROM facilities WHERE short_code = '1MH'), NOW() - INTERVAL '10 minutes');

-- Visit 10: Visit with no assigned officer for 'Currently InTreatment' count
INSERT INTO visits (patient_id, brief_reason, status, checked_in_by_user_id, facility_id, visit_timestamp) VALUES
((SELECT patient_id FROM patients WHERE force_number = 'PTE10000001'), 'Ongoing care after procedure', 'InTreatment',
 (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_1'), (SELECT facility_id FROM facilities WHERE short_code = '2MH'), NOW() - INTERVAL '3 hours');

 -- Visit 11: Discharged Patient for Michael Chen (for sick notes and historical data)
INSERT INTO visits (patient_id, brief_reason, status, checked_in_by_user_id, assigned_officer_user_id, facility_id, visit_timestamp) VALUES
((SELECT patient_id FROM patients WHERE force_number = 'SGT10000002'), 'Flu symptoms, recovered', 'Discharged',
 (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_patientadmin_1'), (SELECT user_id FROM users WHERE logto_sub = 'logto_sub_doctor_1'),
 (SELECT facility_id FROM facilities WHERE short_code = '2MH'), NOW() - INTERVAL '7 days'); -- Set a time far enough in the past