-- 20250601_0304_DoctorNurse_AddFkForPatientMedicalHistory.sql
-- Adds foreign key constraints for the app.patient_medical_history table.

SET search_path TO app, public;

ALTER TABLE patient_medical_history
    ADD CONSTRAINT fk_pat_medical_history_patient_id FOREIGN KEY (patient_id) REFERENCES patients(patient_id),
    ADD CONSTRAINT fk_pat_medical_history_recorded_by_user_id FOREIGN KEY (recorded_by_user_id) REFERENCES users(user_id);