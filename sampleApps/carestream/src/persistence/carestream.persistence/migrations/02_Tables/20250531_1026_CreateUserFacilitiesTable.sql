-- 20250531_1026_CreateUserFacilitiesTable.sql
-- Creates the app.user_facilities junction table.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS user_facilities (
    user_id INT NOT NULL,
    facility_id INT NOT NULL,
    is_default BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id INT NULL,
    PRIMARY KEY (user_id, facility_id)
);