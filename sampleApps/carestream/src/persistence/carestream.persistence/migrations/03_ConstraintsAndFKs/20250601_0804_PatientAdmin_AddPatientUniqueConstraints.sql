-- 20250601_0804_PatientAdmin_AddPatientUniqueConstraints.sql
-- Adds unique constraint for force_number in app.patients.

SET search_path TO app, public;

ALTER TABLE patients ADD CONSTRAINT uq_patients_force_number UNIQUE (force_number);