-- 20250531_1004_CreateStaffReportsTable.sql
-- Creates the app.staff_reports table with basic columns and PK.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS staff_reports (
    report_id SERIAL PRIMARY KEY,
    author_user_id INT NOT NULL,
    title VARCHAR(255) NOT NULL,
    priority VARCHAR(50),
    content TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    facility_id INT NOT NULL,
    department_id INT NULL
);