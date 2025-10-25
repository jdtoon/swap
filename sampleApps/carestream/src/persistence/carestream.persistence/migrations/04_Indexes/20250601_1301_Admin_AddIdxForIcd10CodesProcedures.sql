-- 20250601_1301_Admin_AddIdxForIcd10CodesProcedures.sql
-- Adds indexes for medical codes and procedures.

SET search_path TO app, public;

CREATE INDEX ix_icd10_codes_code ON icd10_codes (code);
CREATE INDEX ix_procedures_code ON procedures (code);
CREATE INDEX ix_procedures_name ON procedures (name);