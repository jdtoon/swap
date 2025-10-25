-- 20250601_0809_DoctorNurse_AddVisitDiagnosisUniqueConstraints.sql
-- Adds unique constraint to ensure an ICD-10 code is unique per visit in app.visit_diagnoses.

SET search_path TO app, public;

ALTER TABLE visit_diagnoses ADD CONSTRAINT uq_visit_diagnosis UNIQUE (visit_id, icd10_code_id);