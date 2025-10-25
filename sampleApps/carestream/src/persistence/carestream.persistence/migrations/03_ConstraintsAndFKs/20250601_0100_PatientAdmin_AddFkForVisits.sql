-- 20250601_0100_PatientAdmin_AddFkForVisits.sql
-- Adds foreign key constraints for the app.visits table.

SET search_path TO app, public;

ALTER TABLE visits
    ADD CONSTRAINT fk_visits_patient_id FOREIGN KEY (patient_id) REFERENCES patients(patient_id),
    ADD CONSTRAINT fk_visits_checked_in_by_user_id FOREIGN KEY (checked_in_by_user_id) REFERENCES users(user_id),
    ADD CONSTRAINT fk_visits_assigned_officer_user_id FOREIGN KEY (assigned_officer_user_id) REFERENCES users(user_id),
    ADD CONSTRAINT fk_visits_facility_id FOREIGN KEY (facility_id) REFERENCES facilities(facility_id);