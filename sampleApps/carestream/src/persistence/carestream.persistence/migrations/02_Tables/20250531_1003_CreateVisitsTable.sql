-- 20250531_1003_CreateVisitsTable.sql
-- Creates the app.visits table with basic columns and PK.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS visits (
    visit_id SERIAL PRIMARY KEY,
    patient_id INT NOT NULL,
    visit_timestamp TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    brief_reason TEXT,
    status VARCHAR(50) NOT NULL,
    checked_in_by_user_id INT NULL,
    assigned_officer_user_id INT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    doctor_consultation_notes TEXT NULL,
    additional_notes TEXT NULL,
    facility_id INT NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);