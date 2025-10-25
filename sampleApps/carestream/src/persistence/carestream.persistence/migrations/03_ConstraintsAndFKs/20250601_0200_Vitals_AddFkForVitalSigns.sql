-- 20250601_0200_Vitals_AddFkForVitalSigns.sql
-- Adds foreign key constraints for the app.vital_signs table.

SET search_path TO app, public;

ALTER TABLE vital_signs
    ADD CONSTRAINT fk_vital_signs_visit_id FOREIGN KEY (visit_id) REFERENCES visits(visit_id),
    ADD CONSTRAINT fk_vital_signs_patient_id FOREIGN KEY (patient_id) REFERENCES patients(patient_id),
    ADD CONSTRAINT fk_vital_signs_recorded_by_user_id FOREIGN KEY (recorded_by_user_id) REFERENCES users(user_id);