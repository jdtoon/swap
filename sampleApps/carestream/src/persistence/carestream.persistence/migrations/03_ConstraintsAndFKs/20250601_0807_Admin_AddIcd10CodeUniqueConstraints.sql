-- 20250601_0807_Admin_AddIcd10CodeUniqueConstraints.sql
-- Adds unique constraint for the code in app.icd10_codes.

SET search_path TO app, public;

ALTER TABLE icd10_codes ADD CONSTRAINT uq_icd10_code UNIQUE (code);