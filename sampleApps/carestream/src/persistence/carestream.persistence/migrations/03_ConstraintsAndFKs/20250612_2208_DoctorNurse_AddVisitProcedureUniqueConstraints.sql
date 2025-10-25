-- 20250612_2208_DoctorNurse_AddVisitProcedureUniqueConstraints.sql
-- Adds unique constraint to ensure an procedure code is unique per visit in app.visit_procedures.

SET search_path TO app, public;

ALTER TABLE visit_procedures ADD CONSTRAINT uq_visit_procedure UNIQUE (visit_id, procedure_id);