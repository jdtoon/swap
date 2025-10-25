-- 20250601_1201_Pharmacy_AddIdxForMedicationStock_Facility.sql
-- Adds an index for medication stock lookup by facility.

SET search_path TO app, public;

-- The primary key (medication_id, facility_id) already creates an index on these columns.
-- This index helps if you often query just by facility_id to see all stock for a facility,
-- or to find low stock items within a facility.
CREATE INDEX ix_medication_stock_facility_id ON medication_stock (facility_id);
CREATE INDEX ix_medication_stock_low_stock ON medication_stock (facility_id, quantity_on_hand) WHERE quantity_on_hand <= minimum_stock_level;