-- 20250601_0101_PatientAdmin_AddFkForPatients.sql
-- Adds foreign key constraints for user and update tracking in app.patients.

SET search_path TO app, public;

ALTER TABLE patients
    ADD CONSTRAINT fk_patients_user_id FOREIGN KEY (user_id) REFERENCES users(user_id),
    ADD CONSTRAINT fk_patients_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users(user_id);