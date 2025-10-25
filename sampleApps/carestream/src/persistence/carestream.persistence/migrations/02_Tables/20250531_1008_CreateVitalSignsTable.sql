-- 20250531_1008_CreateVitalSignsTable.sql
-- Creates the app.vital_signs table.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS vital_signs (
    vital_signs_id SERIAL PRIMARY KEY,
    visit_id INT NOT NULL,
    patient_id INT NOT NULL,
    blood_pressure_systolic INT NULL,
    blood_pressure_diastolic INT NULL,
    heart_rate INT NULL,
    temperature DECIMAL(4,1) NULL,
    respiratory_rate INT NULL,
    oxygen_saturation INT NULL,
    pain_level INT NULL,
    urinalysis_color VARCHAR(50) NULL,
    urinalysis_clarity VARCHAR(50) NULL,
    urinalysis_specific_gravity DECIMAL(5,3) NULL,
    urinalysis_ph DECIMAL(3,1) NULL,
    urinalysis_protein VARCHAR(50) NULL,
    urinalysis_glucose VARCHAR(50) NULL,
    clinical_notes TEXT NULL,
    requires_follow_up BOOLEAN NOT NULL DEFAULT FALSE,
    mark_as_urgent BOOLEAN NOT NULL DEFAULT FALSE,
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    recorded_by_user_id INT NULL
);