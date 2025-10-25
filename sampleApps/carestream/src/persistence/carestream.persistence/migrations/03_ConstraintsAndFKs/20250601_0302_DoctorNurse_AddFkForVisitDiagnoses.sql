-- 20250601_0302_DoctorNurse_AddFkForVisitDiagnoses.sql
-- Adds foreign key constraints for the app.visit_diagnoses table.

SET search_path TO app, public;

ALTER TABLE visit_diagnoses
    ADD CONSTRAINT fk_visit_diagnoses_visit_id FOREIGN KEY (visit_id) REFERENCES visits(visit_id),
    ADD CONSTRAINT fk_visit_diagnoses_icd10_code_id FOREIGN KEY (icd10_code_id) REFERENCES icd10_codes(icd10_code_id),
    ADD CONSTRAINT fk_visit_diagnoses_recorded_by_user_id FOREIGN KEY (recorded_by_user_id) REFERENCES users(user_id);