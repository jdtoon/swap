-- 20250531_1006_CreateDepartmentsTable.sql
-- Creates the app.departments table.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS departments (
    department_id SERIAL PRIMARY KEY,
    facility_id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id INT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_by_user_id INT NULL
);