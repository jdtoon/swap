-- 20250531_1011_CreateSickNotesTable.sql
-- Creates the app.sick_notes table.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS sick_notes (
    sick_note_id SERIAL PRIMARY KEY,
    visit_id INT NOT NULL,
    start_date DATE NULL,
    end_date DATE NULL,
    diagnosis TEXT NULL,
    recommendations TEXT NULL,
    issued_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    issued_by_user_id INT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);