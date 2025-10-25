-- 20250531_1025_CreateUserRolesTable.sql
-- Creates the app.user_roles table.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS user_roles (
    user_role_id SERIAL PRIMARY KEY,
    user_id INT NOT NULL,
    role_id INT NOT NULL,
    facility_id INT NULL,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    assigned_by_user_id INT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);