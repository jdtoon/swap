-- 20250601_0806_Pharmacy_AddMedicationStockConstraints.sql
-- Adds check constraints for quantity_on_hand in app.medication_stock.
-- Assumes medication_stock_id and facility_id are already part of a composite PRIMARY KEY.

SET search_path TO app, public;

-- The composite PK (medication_id, facility_id) should be defined in the schema creation script for medication_stock (20250531_1013).
-- So we just add the check constraint here.
ALTER TABLE medication_stock ADD CONSTRAINT chk_medication_stock_quantity_on_hand CHECK (quantity_on_hand >= 0);