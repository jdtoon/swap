-- 20250531_1017_CreateVisitProceduresTable.sql
-- Creates the app.visit_procedures table.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS visit_procedures (
    visit_procedure_id SERIAL PRIMARY KEY,
    visit_id INT NOT NULL,
    procedure_id INT NOT NULL,
    performed_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    performed_by_user_id INT NULL,
    notes TEXT NULL
);