-- 20250531_1012_CreateDispensationLogItemsTable.sql
-- Creates the app.dispensation_log_items table.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS dispensation_log_items (
    dispensation_log_item_id SERIAL PRIMARY KEY,
    prescription_item_id INT NOT NULL,
    visit_id INT NOT NULL,
    medication_id INT NOT NULL,
    quantity_dispensed_transaction VARCHAR(50) NOT NULL,
    dispensed_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    dispensed_by_user_id INT NOT NULL,
    pharmacist_notes TEXT NULL,
    batch_number VARCHAR(100) NULL,
    expiry_date DATE NULL
);