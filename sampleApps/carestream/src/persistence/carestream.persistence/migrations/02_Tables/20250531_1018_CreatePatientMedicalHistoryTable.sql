-- 20250531_1018_CreatePatientMedicalHistoryTable.sql
-- Creates the app.patient_medical_history table.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS patient_medical_history (
    history_id SERIAL PRIMARY KEY,
    patient_id INT NOT NULL,
    type VARCHAR(100) NOT NULL,
    description TEXT NOT NULL,
    onset_date DATE NULL,
    resolution_date DATE NULL,
    severity VARCHAR(50) NULL,
    notes TEXT NULL,
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    recorded_by_user_id INT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);