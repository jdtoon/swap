-- 20250531_1010_CreatePrescriptionItemsTable.sql
-- Creates the app.prescription_items table.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS prescription_items (
    prescription_item_id SERIAL PRIMARY KEY,
    visit_id INT NOT NULL,
    medication_id INT NOT NULL,
    dosage VARCHAR(100) NOT NULL,
    frequency VARCHAR(100) NOT NULL,
    duration VARCHAR(100) NULL,
    quantity_prescribed VARCHAR(50) NOT NULL,
    special_instructions TEXT NULL,
    is_sent_to_pharmacy BOOLEAN NOT NULL DEFAULT FALSE,
    pharmacy_sent_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id INT NULL,
    quantity_dispensed VARCHAR(50) NULL,
    is_fully_dispensed BOOLEAN NOT NULL DEFAULT FALSE,
    last_dispensed_at TIMESTAMPTZ NULL,
    last_dispensed_by_user_id INT NULL
);