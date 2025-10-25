-- 20250531_1014_CreateIcd10CodesTable.sql
-- Creates the app.icd10_codes table.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS icd10_codes (
    icd10_code_id SERIAL PRIMARY KEY,
    code VARCHAR(10) NOT NULL,
    description TEXT NOT NULL,
    category VARCHAR(100) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id INT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_by_user_id INT NULL
);