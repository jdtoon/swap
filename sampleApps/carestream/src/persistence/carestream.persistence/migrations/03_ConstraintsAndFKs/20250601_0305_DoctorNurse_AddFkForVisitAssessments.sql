-- 20250601_0305_DoctorNurse_AddFkForVisitAssessments.sql
-- Adds foreign key constraints for the app.visit_assessments table.

SET search_path TO app, public;

ALTER TABLE visit_assessments
    ADD CONSTRAINT fk_visit_assessments_visit_id FOREIGN KEY (visit_id) REFERENCES visits(visit_id),
    ADD CONSTRAINT fk_visit_assessments_patient_id FOREIGN KEY (patient_id) REFERENCES patients(patient_id),
    ADD CONSTRAINT fk_visit_assessments_assessed_by_user_id FOREIGN KEY (assessed_by_user_id) REFERENCES users(user_id);