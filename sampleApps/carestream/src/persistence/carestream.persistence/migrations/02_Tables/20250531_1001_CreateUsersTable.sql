-- 20250531_1001_CreateUsersTable.sql
-- Creates the app.users table with basic columns and PK.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS users (
    user_id SERIAL PRIMARY KEY,
    logto_sub VARCHAR(100),
    force_number VARCHAR(50),
    rank VARCHAR(100),
    first_name VARCHAR(150) NOT NULL,
    last_name VARCHAR(150) NOT NULL,
    department VARCHAR(100), -- Will become FK later
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    hashed_verification_code VARCHAR(255) NULL,
    verification_code_salt VARCHAR(100) NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);