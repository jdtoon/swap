-- 20250531_1013_CreateMedicationStockTable.sql
-- Creates the app.medication_stock table with composite PK for multi-tenancy.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS medication_stock (
    medication_id INT NOT NULL,
    facility_id INT NOT NULL,
    quantity_on_hand INT NOT NULL DEFAULT 0,
    minimum_stock_level INT NOT NULL DEFAULT 10,
    last_updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (medication_id, facility_id) -- NEW: Composite Primary Key
);