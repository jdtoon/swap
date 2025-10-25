-- 20250531_1007_CreateWardsTable.sql
-- Creates the app.wards table.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS wards (
    ward_id SERIAL PRIMARY KEY,
    facility_id INT NOT NULL,
    department_id INT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id INT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_by_user_id INT NULL
);