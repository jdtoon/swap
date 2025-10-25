-- 20250601_0900_PatientAdmin_AddIdxForPatients_Names.sql
-- Adds an index for patient name search.

SET search_path TO app, public;

CREATE INDEX ix_patients_names ON patients (last_name, first_name);