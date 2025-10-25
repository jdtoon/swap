-- 20250601_0901_PatientAdmin_AddIdxForPatients_ForceNumber.sql
-- Adds an index for patient force_number lookup.

SET search_path TO app, public;

CREATE INDEX ix_patients_force_number ON patients (force_number);