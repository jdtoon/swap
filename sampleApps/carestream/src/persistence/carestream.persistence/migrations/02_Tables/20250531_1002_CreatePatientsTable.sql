-- 20250531_1002_CreatePatientsTable.sql
-- Creates the app.patients table with basic columns and PK.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS patients (
    patient_id SERIAL PRIMARY KEY,
    force_number VARCHAR(50),
    rank VARCHAR(100),
    first_name VARCHAR(150) NOT NULL,
    last_name VARCHAR(150) NOT NULL,
    date_of_birth DATE,
    gender VARCHAR(50),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    user_id INT NULL,
    unit VARCHAR(100) NULL,
    email_address VARCHAR(255) NULL,
    primary_phone_number VARCHAR(50) NULL,
    emergency_contact_name VARCHAR(255) NULL,
    emergency_contact_phone VARCHAR(50) NULL,
    address_line1 VARCHAR(255) NULL,
    address_line2 VARCHAR(255) NULL,
    city VARCHAR(100) NULL,
    province VARCHAR(100) NULL,
    postal_code VARCHAR(20) NULL,
    country VARCHAR(100) NOT NULL DEFAULT 'South Africa',
    next_of_kin_name VARCHAR(255) NULL,
    next_of_kin_phone VARCHAR(50) NULL,
    next_of_kin_relationship VARCHAR(100) NULL,
    religion VARCHAR(100) NULL,
    occupation VARCHAR(100) NULL,
    marital_status VARCHAR(50) NULL,
    nationality VARCHAR(100) NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_by_user_id INT NULL
);