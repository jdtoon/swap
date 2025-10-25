-- 20250531_1020_CreateReferralsTable.sql
-- Creates the app.referrals table.

SET search_path TO app, public;

CREATE TABLE IF NOT EXISTS referrals (
    referral_id SERIAL PRIMARY KEY,
    visit_id INT NOT NULL,
    patient_id INT NOT NULL,
    referred_by_user_id INT NOT NULL,
    referred_to_department_id INT NULL,
    referred_to_facility_id INT NULL,
    referral_reason TEXT NOT NULL,
    referral_notes TEXT NULL,
    referral_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    completed_date TIMESTAMPTZ NULL,
    completed_by_user_id INT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);