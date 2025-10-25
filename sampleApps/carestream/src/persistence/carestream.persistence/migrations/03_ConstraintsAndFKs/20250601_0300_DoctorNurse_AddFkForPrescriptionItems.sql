-- 20250601_0300_DoctorNurse_AddFkForPrescriptionItems.sql
-- Adds foreign key constraints for the app.prescription_items table.

SET search_path TO app, public;

ALTER TABLE prescription_items
    ADD CONSTRAINT fk_prescription_items_visit_id FOREIGN KEY (visit_id) REFERENCES visits(visit_id),
    ADD CONSTRAINT fk_prescription_items_medication_id FOREIGN KEY (medication_id) REFERENCES medications(medication_id),
    ADD CONSTRAINT fk_prescription_items_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users(user_id),
    ADD CONSTRAINT fk_prescription_items_last_dispensed_by_user_id FOREIGN KEY (last_dispensed_by_user_id) REFERENCES users(user_id);