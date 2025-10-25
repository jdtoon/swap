-- 20250531_1009_CreateMedicationsTable.sql
-- Creates the app.medications table.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS medications (
    medication_id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    strength VARCHAR(100) NULL,
    form VARCHAR(100) NULL,
    category VARCHAR(100) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);