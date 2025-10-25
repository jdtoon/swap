-- 20250601_0306_DoctorNurse_AddFkForReferrals.sql
-- Adds foreign key constraints for the app.referrals table.

SET search_path TO app, public;

ALTER TABLE referrals
    ADD CONSTRAINT fk_referrals_visit_id FOREIGN KEY (visit_id) REFERENCES visits(visit_id),
    ADD CONSTRAINT fk_referrals_patient_id FOREIGN KEY (patient_id) REFERENCES patients(patient_id),
    ADD CONSTRAINT fk_referrals_referred_by_user_id FOREIGN KEY (referred_by_user_id) REFERENCES users(user_id),
    ADD CONSTRAINT fk_referrals_referred_to_department_id FOREIGN KEY (referred_to_department_id) REFERENCES departments(department_id),
    ADD CONSTRAINT fk_referrals_referred_to_facility_id FOREIGN KEY (referred_to_facility_id) REFERENCES facilities(facility_id),
    ADD CONSTRAINT fk_referrals_completed_by_user_id FOREIGN KEY (completed_by_user_id) REFERENCES users(user_id);