-- 20250601_1103_DoctorNurse_AddIdxForPatientMedicalHistory_PatientId.sql
-- Adds an index for patient medical history lookup.

SET search_path TO app, public;

CREATE INDEX ix_patient_medical_history_patient_id ON patient_medical_history (patient_id);