-- 20250531_1016_CreateVisitDiagnosesTable.sql
-- Creates the app.visit_diagnoses table.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS visit_diagnoses (
    visit_diagnosis_id SERIAL PRIMARY KEY,
    visit_id INT NOT NULL,
    icd10_code_id INT NOT NULL,
    diagnosis_type VARCHAR(50) NULL,
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    recorded_by_user_id INT NULL
);