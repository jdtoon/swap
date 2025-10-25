-- 20250601_0808_Admin_AddProcedureUniqueConstraints.sql
-- Adds unique constraint for the code in app.procedures.

SET search_path TO app, public;

ALTER TABLE procedures ADD CONSTRAINT uq_procedures_code UNIQUE (code);