-- 20250531_1019_CreateVisitAssessmentsTable.sql
-- Creates the app.visit_assessments table.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS visit_assessments (
    visit_assessment_id SERIAL PRIMARY KEY,
    visit_id INT NOT NULL,
    patient_id INT NOT NULL,
    assessment_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    assessed_by_user_id INT NULL,
    physical_exam_findings TEXT NULL,
    cardiovascular_notes TEXT NULL,
    respiratory_notes TEXT NULL,
    musculoskeletal_notes TEXT NULL,
    neurological_notes TEXT NULL,
    psychological_notes TEXT NULL,
    other_systems_notes TEXT NULL,
    medical_classification VARCHAR(100) NULL,
    deployment_status VARCHAR(100) NULL,
    validity_period_months INT NULL,
    restrictions TEXT NULL
);