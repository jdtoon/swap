-- 20250601_0400_Pharmacy_AddFkForDispensationLogItems.sql
-- Adds foreign key constraints for the app.dispensation_log_items table.

SET search_path TO app, public;

ALTER TABLE dispensation_log_items
    ADD CONSTRAINT fk_disp_log_prescription_item_id FOREIGN KEY (prescription_item_id) REFERENCES prescription_items(prescription_item_id),
    ADD CONSTRAINT fk_disp_log_visit_id FOREIGN KEY (visit_id) REFERENCES visits(visit_id),
    ADD CONSTRAINT fk_disp_log_medication_id FOREIGN KEY (medication_id) REFERENCES medications(medication_id),
    ADD CONSTRAINT fk_disp_log_dispensed_by_user_id FOREIGN KEY (dispensed_by_user_id) REFERENCES users(user_id);