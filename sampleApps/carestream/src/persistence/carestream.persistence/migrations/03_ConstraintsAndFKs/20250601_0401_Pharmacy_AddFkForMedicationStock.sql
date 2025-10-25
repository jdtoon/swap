-- 20250601_0401_Pharmacy_AddFkForMedicationStock.sql
-- Adds foreign key constraints for the app.medication_stock table.

SET search_path TO app, public;

ALTER TABLE medication_stock
    ADD CONSTRAINT fk_medication_stock_medication_id FOREIGN KEY (medication_id) REFERENCES medications(medication_id),
    ADD CONSTRAINT fk_medication_stock_facility_id FOREIGN KEY (facility_id) REFERENCES facilities(facility_id); -- NEW: Added FK for facility_id